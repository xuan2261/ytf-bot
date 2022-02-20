using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
        public static string WorkFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testYoutubeApiWorkDir");

        /// <summary>
        /// Returns a valid youtube api for testing. There is a special api key only for testing for avoiding to reduce the quota.
        /// </summary>
        /// <param name="localLogger">Creates a logger for tests.</param>
        /// <returns>Valid youtube api.</returns>
        private static YoutubeApi.YoutubeApi SetUpTest(out Logger localLogger)
        {
            if (Directory.Exists(WorkFolder))
            {
                // Clean dir
                var di = new DirectoryInfo(WorkFolder);
                foreach (var file in di.EnumerateFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(WorkFolder);
            }

            localLogger = new Logger("yt_test.log");
            localLogger.LogDebug("Test was set up.");

            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi(botConfig.YoutubeConfig.ApiKey4Testing, WorkFolder, localLogger);
            return youtubeApi;
        }

        private static Channel GetTestChannel()
        {
            return new Channel
            {
                ChannelId = "UCOCZKlOz6cNs2qiIhls5cqQ",
                ChannelUploadsPlayListId = "UUOCZKlOz6cNs2qiIhls5cqQ",
                ChannelName = "Njal"
            };
        }

        private static Channel GetTestChannel2()
        {
            return new Channel
            {
                ChannelId = "UCraxywJxOEv-zQ2Yvmp4LtA",
                ChannelUploadsPlayListId = "UUraxywJxOEv-zQ2Yvmp4LtA",
                ChannelName = "Njals Traum - Thema"
            };
        }

        private static Channel GetVictimTestChannel()
        {
            return new Channel
            {
                ChannelId = "UCHvSmXEhCne99aKmiNeSiBQ",
                ChannelUploadsPlayListId = "UUHvSmXEhCne99aKmiNeSiBQ",
                ChannelName = "Symphonic Black Metal Promotion II"
            };
        }

        /// <summary>
        /// Test for GetFullVideoMetaDataOfChannelAsync
        /// </summary>
        [TestMethod]
        public void CheckThatSpecialVictimsChannel()
        {
            Channel victimChannel = GetVictimTestChannel();
            var youtubeApi = SetUpTest(out Logger localLogger);

            var theTaskBaby = youtubeApi.GetFullVideoMetaDataOfChannelAsync(victimChannel, 10);
            theTaskBaby.Wait();
            var listOfVideos = theTaskBaby.Result;
            Assert.AreEqual(listOfVideos.Count, 10);
        }

        /// <summary>
        /// Test for GetVideoMetaData.
        /// </summary>
        [TestMethod]
        public void CheckVideoDataOfAPremiereVideo()
        {
            var youtubeApi = SetUpTest(out Logger myLogger);
            var id = "0OPI5qnpIEE";
            var task = youtubeApi.GetVideoMetaData(id);
            task.Wait();
            var videoData = task.Result;
            Assert.AreEqual(videoData.Id, id);
            Assert.AreEqual(videoData.ChannelTitle, "Njal");
        }

        /// <summary>
        /// Tests CreateListWithFullVideoMetaDataAsync.
        /// 
        /// </summary>
        [TestMethod]
        public void TestIfListWasReturned()
        {
            var youtubeApi = SetUpTest(out var localLogger);
            var channelList = new List<Channel> { GetTestChannel(), GetTestChannel2() };
            var testList = youtubeApi.CreateListWithFullVideoMetaDataAsync(channelList, 10).Result;
            Assert.IsTrue(testList.Count >= 9);
        }

        /// <summary>
        /// Test for StartYoutubeWorker in YtManager.
        /// </summary>
        [TestMethod]
        public void StartYoutubeWorkerTest()
        {
            var youtubeApi = SetUpTest(out var localLogger);
            var theTestChannel = GetTestChannel();
            var secondChannel = GetTestChannel2();

            void MyLocalCallback(string arg1, string message)
            {
                Assert.IsFalse(arg1 == ""); // Must not happen

                localLogger.LogDebug($"Callback was called first arg: {arg1}, second arg: {message}");
            }

            var channelList = new List<Channel>
                              {
                                  theTestChannel,
                                  secondChannel
                              };

            var ytManager = new YtManager(youtubeApi, WorkFolder);
            var ddd = ytManager.StartYoutubeWorker(channelList, 2, MyLocalCallback);
            localLogger.LogDebug("Just started Youtube Worker. Now wait 10 Seconds.");
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Stopped Youtube Worker and wait another 10 Seconds.");
            ytManager.StopYoutubeWorker();
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Done very well. If theres no exception you're good.");

            Assert.AreEqual(Directory.GetFiles(WorkFolder).Length, 4);
        }


        /// <summary>
        /// Test for StartYoutubeWorker in YtManager.
        /// </summary>
        [TestMethod]
        public void StartYoutubeWorkerAndUpdateRollingFileTest()
        {
            var youtubeApi = SetUpTest(out var localLogger);

            for (int i = 0; i < 10; i++)
            {
                File.WriteAllText(Path.Combine(WorkFolder, $"{i:D2}.video"), $"Irgenebbes {i:D2}");
            }

            var theTestChannel = GetTestChannel();
            var secondChannel = GetTestChannel2();

            void MyLocalCallback(string arg1, string message)
            {
                Assert.IsFalse(arg1 == ""); // Must not happen

                localLogger.LogDebug($"Callback was called first arg: {arg1}, second arg: {message}");
            }

            var channelList = new List<Channel>
                              {
                                  theTestChannel,
                                  secondChannel
                              };
            Assert.AreEqual(Directory.GetFiles(WorkFolder).Length, 10);
            var ytManager = new YtManager(youtubeApi, WorkFolder);
            var ddd = ytManager.StartYoutubeWorker(channelList, 2, MyLocalCallback);
            localLogger.LogDebug("Just started Youtube Worker. Now wait 10 Seconds.");
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Stopped Youtube Worker and wait another 10 Seconds.");
            ytManager.StopYoutubeWorker();
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Done very well. If theres no exception you're good.");

            Assert.AreEqual(Directory.GetFiles(WorkFolder).Length, 6);
        }

    }
}