using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleLogger;
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
        /// Returns the test channel and instantiates a youtube api.
        /// </summary>
        /// <remarks>
        /// Important! Before anything can be tested here, it must be ensured that there is a final successful check for published
        /// videos of a channel. This means that this method also creates a file with a timestamp that is far in the past. In the
        /// channel used here, all tests should return all videos of the channel.
        /// </remarks>
        /// <param name="youtubeApi">The instantiated youtube Api</param>
        /// <returns></returns>
        private static Channel SetUpTest(out YoutubeApi.YoutubeApi youtubeApi)
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            Channel theTestChannel = new Channel
                                     {
                                         ChannelId = "UCOCZKlOz6cNs2qiIhls5cqQ",
                                         ChannelName = "Njal"
                                     };
            var logger = new Logger("yt_test.log");
            youtubeApi = new YoutubeApi.YoutubeApi("TestApp", botConfig.YoutubeConfig.ApiKey, logger);
            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(theTestChannel, new DateTime(2021, 1, 1));
            return theTestChannel;
        }

        /// <summary>
        /// Yes, this no UnitTests and it sucks. I had not the time to.
        /// This test calls the main method of the YoutubeApi 'CreateListWithFullVideoMetaDataAsync'. The channel that is written
        /// to the list in the 'SetUpTest' method is tested.
        /// And yes, right again: the test sucks and has to be adjusted as soon as I publish the next video.
        /// ///
        /// </summary>
        [TestMethod]
        public void TestIfListWasReturned()
        {
            var theTestChannel = SetUpTest(out var youtubeApi);

            var channelList = new List<Channel> { theTestChannel };
            var testList = youtubeApi.CreateListWithFullVideoMetaDataAsync(channelList, 10).Result;

            Assert.IsTrue(testList.Count >= 6);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void StartYoutubeWorkerTest()
        {
            string thefile = string.Empty;

            void MyLocalCallback(string file, string message)
            {
                Assert.IsFalse(file == "");
                if (file != "End")
                {
                    thefile = file;
                }

                Console.WriteLine(file);
                Console.WriteLine(message);
            }

            var theTestChannel = SetUpTest(out var youtubeApi);
            var secondChannel = new Channel
                                {
                                    ChannelId = "UCraxywJxOEv-zQ2Yvmp4LtA",
                                    ChannelName = "Njals Traum - Thema"
                                };
            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(secondChannel, new DateTime(2021, 1, 1));
            var channelList = new List<Channel>
                              {
                                  theTestChannel,
                                  secondChannel
                              };
            

            var ytManager = new YtManager(youtubeApi);
            var ddd = ytManager.StartYoutubeWorkerWorker(channelList, MyLocalCallback);
            ddd.Wait(TimeSpan.FromMinutes(1));
            ytManager.StopYoutubeWorker();
            ddd.Wait(TimeSpan.FromSeconds(35));
            Assert.IsTrue(File.Exists(thefile));
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith("Full_Meta_YT.json"))
                .ToList()
                .ForEach(File.Delete);
        }
    }
}