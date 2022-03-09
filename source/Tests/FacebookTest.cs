﻿using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using FacebookAutomation;
using FbAutomation = FacebookAutomation.FacebookAutomation;

namespace Tests
{
    [TestClass]
    public class FacebookTest
    {
        public string WorkFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testFacebookWorkDir");

        /// <summary>
        /// Get a real video description for Testing the FacebookAutomation
        /// </summary>
        /// <returns></returns>
        public VideoMetaDataFull GetVideometaData(string videoId)
        {
            var localLogger = new Logger("yt_test.log");
            localLogger.LogDebug("Test was set up.");
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi(botConfig.YoutubeConfig.ApiKey4Testing, WorkFolder, localLogger);

            return youtubeApi.GetVideoMetaData(videoId).Result;
        }

        /// <summary>
        /// Use FacebookAutomation to publish in test group.
        /// </summary>
        [TestMethod]
        public void SimpleLogInAndPostToGroup()
        {
            var theMessage = GetVideometaData("79VxD19n9HI").GetFacebookDescription();

            var logger = new Logger("TestFacebookLogFile.log");
            var facebook = new FbAutomation(WorkFolder, logger);
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;

            facebook.Login(facebookConfig.Email, facebookConfig.Pw);
            facebook.PublishTextContentInFaceBookGroup(facebookConfig.TestGroups[0].GroupId, theMessage);
            facebook.Dispose();
        }

        /// <summary>
        /// Use Fbmanager to publish in groups.
        /// Set up scenario with a list file and 2 videos of 2 different channels.
        /// FbManager will publish 2 videos in 2 groups --> 4 posts in one chrome window.
        /// </summary>
        [TestMethod]
        public void TestFbManagerSendVideoMetaDataTo2GroupsAsync()
        {
            FileHandlingTest.SetupTest(WorkFolder);
            var firstVideo = GetVideometaData("ZWHBsKm9Egk");
            var secondVideo = GetVideometaData("0B_0HWfG96I");
            VideoMetaDataFull.SerializeToFileInSubfolder(firstVideo, WorkFolder);
            VideoMetaDataFull.SerializeToFileInSubfolder(secondVideo, WorkFolder);
            var pathToListFile = Path.Combine(WorkFolder, "__TheTestListFile.list");
            File.WriteAllText(pathToListFile, string.Empty);
            var listOfFileNames = FileHandling.FindNotYetProcessedVideoIdFiles(pathToListFile, WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);

            var logger = new Logger("TestFacebookLogFile.log");
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;
            
            var fbManager = new FbManager(WorkFolder, facebookConfig);
            fbManager.SendVideoMetaDataToGroupsAsync(listOfFileNames, facebookConfig.TestGroups.Take(2).ToList(), false).Wait();
        }

        /// <summary>
        /// Use Fbmanager to publish in groups.
        /// Set up scenario with a list file and 2 videos of 2 different channels.
        /// FbManager will publish 2 videos in 4 groups --> 8 posts in one chrome window.
        /// </summary>
        [TestMethod]
        public void TestFbManagerSendVideoMetaDataTo4GroupsAsync()
        {
            FileHandlingTest.SetupTest(WorkFolder);
            var firstVideo = GetVideometaData("ZWHBsKm9Egk");
            var secondVideo = GetVideometaData("0B_0HWfG96I");
            VideoMetaDataFull.SerializeToFileInSubfolder(firstVideo, WorkFolder);
            VideoMetaDataFull.SerializeToFileInSubfolder(secondVideo, WorkFolder);
            var logger = new Logger("TestFacebookLogFile.log");
            var tasks = new List<Task>();
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;

            // First 2 groups
            var pathToListFile1 = Path.Combine(WorkFolder, "__TheTestListFile_01.list");
            File.WriteAllText(pathToListFile1, string.Empty);
            var listOfFileNames1 = FileHandling.FindNotYetProcessedVideoIdFiles(pathToListFile1, WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            var fbManager1 = new FbManager(WorkFolder, facebookConfig);
            tasks.Add(fbManager1.SendVideoMetaDataToGroupsAsync(listOfFileNames1, facebookConfig.TestGroups, false));

            Task.WaitAll(tasks.ToArray());
        }
    }
}
