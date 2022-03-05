using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleLogger;
using System;
using System.IO;
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
        public string GetVideoDescription()
        {
            var videoId = "ZWHBsKm9Egk";

            var localLogger = new Logger("yt_test.log");
            localLogger.LogDebug("Test was set up.");
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi(botConfig.YoutubeConfig.ApiKey4Testing, WorkFolder, localLogger);

            var video = youtubeApi.GetVideoMetaData(videoId).Result;

            return video.GetFacebookDescription();
        }

        [TestMethod]
        public void SimpleLogInAndPostToGroup()
        {
            var theMessage = GetVideoDescription();

            var logger = new Logger("TestFacebookLogFile.log");
            var facebook = new FbAutomation(WorkFolder, logger);
            var facebookConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").FacebookConfig;

            facebook.Login(facebookConfig.Email, facebookConfig.Pw);
            facebook.PublishTextContentInFaceBookGroup(facebookConfig.Groups[0].GroupId, theMessage);

        }
    }
}
