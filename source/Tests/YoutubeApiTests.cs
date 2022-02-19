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
        public string WorkFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testYoutubeApiWorkDir");

        /// <summary>
        /// Returns a valid youtube api for testing. There is a special api key only for testing for avoiding to reduce the quota.
        /// </summary>
        /// <param name="localLogger">Creates a logger for tests.</param>
        /// <returns>Valid youtube api.</returns>
        private static YoutubeApi.YoutubeApi SetUpTest(out Logger localLogger)
        {
            localLogger = new Logger("yt_test.log");
            localLogger.LogDebug("Test was set up.");

            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi("TestApp", botConfig.YoutubeConfig.ApiKey4Testing, localLogger);
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

        private static Channel GetVictimTestChannel()
        {
            return new Channel
            {
                ChannelId = "UCHvSmXEhCne99aKmiNeSiBQ",
                ChannelUploadsPlayListId = "UUHvSmXEhCne99aKmiNeSiBQ",
                ChannelName = "Symphonic Black Metal Promotion II"
            };
        }

        [TestMethod]
        public void CheckThatSpecialVictimsChannel()
        {
            Channel theTestChannel = GetVictimTestChannel();
          
            var youtubeApi = 

            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(theTestChannel, new DateTime(2022, 02, 12, 11, 00, 00));
            var theTaskBaby = youtubeApi.GetFullVideoMetaDataOfChannelAsync(theTestChannel, 10);
            theTaskBaby.Wait();
            var listOfVideos = theTaskBaby.Result;
        }

        [TestMethod]
        public void CheckVideoDataOfAPremiereVideo()
        {
            var channel = SetUpTest(out YoutubeApi.YoutubeApi youtubeApi, out Logger myLogger);

            var task = youtubeApi.GetVideoMetaData("BqsrCVi_5kM");
            task.Wait();
            var videoData = task.Result;
        }

        /// <summary>
        /// Yes, this is no UnitTests and it sucks. I had not the time to.
        /// This test calls the main method of the YoutubeApi 'CreateListWithFullVideoMetaDataAsync'. The channel that is written
        /// to the list in the 'SetUpTest' method is tested.
        /// And yes, right again: the test sucks and has to be adjusted as soon as I publish the next video.
        /// ///
        /// </summary>
        [TestMethod]
        public void TestIfListWasReturned()
        {
            var theTestChannel = SetUpTest(out var youtubeApi, out var localLogger);

            var channelList = new List<Channel> { theTestChannel };
            var testList = youtubeApi.CreateListWithFullVideoMetaDataAsync(channelList, 10).Result;

            Assert.IsTrue(testList.Count >= 6);
        }

        /// <summary>
        /// Test sets back the timestamp file to 1.1.21, so the youtube worker will detect some videos in any case.
        /// Worker should detect about 8 Videos and write it in one video meta file.
        /// </summary>
        [TestMethod]
        public void StartYoutubeWorkerTest()
        {
            var theTestChannel = SetUpTest(out var youtubeApi, out var localLogger);
            var secondChannel = new Channel
            {
                ChannelId = "UCraxywJxOEv-zQ2Yvmp4LtA",
                ChannelUploadsPlayListId = "UUraxywJxOEv-zQ2Yvmp4LtA",
                ChannelName = "Njals Traum - Thema"
            };
            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(secondChannel, new DateTime(2021, 1, 1));

            void MyLocalCallback(string file, string message)
            {
                Assert.IsFalse(file == ""); // Must not happen

                localLogger.LogDebug($"Callback was called first arg: {file}, second arg: {message}");
            }

            var channelList = new List<Channel>
                              {
                                  theTestChannel,
                                  secondChannel
                              };

            var ytManager = new YtManager(youtubeApi, WorkFolder);
            var ddd = ytManager.StartYoutubeWorker(channelList, MyLocalCallback);
            localLogger.LogDebug("Just started Youtube Worker. Now wait 10 Seconds.");
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Stopped Youtube Worker and wait another 10 Seconds.");
            ytManager.StopYoutubeWorker();
            ddd.Wait(TimeSpan.FromSeconds(10));
            localLogger.LogDebug("Done very well. If theres no exception youre good.");

            //Check output folder manually. There was one file created within all videos.
        }
    }
}