using System;
using System.Collections.Generic;
using System.IO;
using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeApi;

namespace Tests
{
    /// <summary>
    /// Yes, these are not unit tests and they don't give much away. And yes, the implementation would have to be significantly
    /// improved and, above all, mocks would have to be used. Any implementation is preferable to any cleverness.
    /// </summary>
    [TestClass]
    public class YoutubeApiTests
    {
        /// <summary>
        /// Yes, this no UnitTests and it sucks. I had not the time to
        /// </summary>
        [TestMethod]
        public void TestIfFileExists()
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");

            var youtubeApi = new YoutubeApi.YoutubeApi("TestApp", botConfig.YoutubeConfig.ApiKey);

            Channel theTestChannel = new Channel
                                     {
                                         ChannelId = "UCOCZKlOz6cNs2qiIhls5cqQ",
                                         ChannelName = "Njal"
                                     };
            var channelList = new List<Channel> { theTestChannel };

            youtubeApi.CreateVideoFileAsync(channelList, new DateTime(2021, 01, 01), 10).Wait(TimeSpan.FromSeconds(60));

            Assert.IsTrue(File.Exists("youtubeVideos.json"));
        }
    }
}