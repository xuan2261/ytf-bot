using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
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
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi(botConfig.YoutubeConfig.ApiKey4Testing, WorkFolder);

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
            facebook.PublishToGroup(facebookConfig.TestGroups[0], theMessage);
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

            var logger = new Logger("TestFacebookLogFile.log");
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;
            
            var fbManager = new FbManager(WorkFolder, facebookConfig);
            fbManager.PrepareAndSendToGroups(pathToListFile, facebookConfig.TestGroups.Take(2).ToList(), false);
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
            var firstVideo = GetVideometaData("ZWHBsKm9Egk"); // german video
            var secondVideo = GetVideometaData("a3rjfQDqQx8");
            VideoMetaDataFull.SerializeToFileInSubfolder(firstVideo, WorkFolder);
            VideoMetaDataFull.SerializeToFileInSubfolder(secondVideo, WorkFolder);
            
            var logger = new Logger("TestFacebookLogFile.log");
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;

            // Publish two videos in 4 groups
            var pathToListFile1 = Path.Combine(WorkFolder, "__TheTestListFile_01.list");
            File.WriteAllText(pathToListFile1, string.Empty);
            var fbManager1 = new FbManager(WorkFolder, facebookConfig);
            fbManager1.PrepareAndSendToGroups(pathToListFile1, facebookConfig.TestGroups, true);
        }

        [TestMethod]
        public void TestGroupMembers()
        {
            var logger = new Logger("TestFacebookLogFile.log");
            var facebook = new FbAutomation(WorkFolder, logger);
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;
            var fbGroupSmallBM = facebookConfig.TaskGroups_smallVinylGroup[0];
            var fbGroupSmallSkyrim = facebookConfig.Skyrimgroups[0];

            facebook.Login(facebookConfig.Email, facebookConfig.Pw);
            var dictSmallVinylGroup = facebook.GetGroupMembers(fbGroupSmallSkyrim);
            facebook.Dispose();


            var serializerOptions = new JsonSerializerOptions
                                    {
                                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                        WriteIndented = true,

                                    };
            File.WriteAllText($"{fbGroupSmallSkyrim.GroupName}.json", JsonSerializer.Serialize(dictSmallVinylGroup, serializerOptions));
        }
    }
}
