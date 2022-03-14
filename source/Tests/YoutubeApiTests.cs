using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BotService;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.DevTools.V85.Page;
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

                foreach (var directory in di.EnumerateDirectories())
                {
                    Directory.Delete(directory.FullName, true);
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
                ChannelName = "Njals Traum - Topic"
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

        private static Channel GetBMPChannel()
        {
            return new Channel
                   {
                       ChannelId = "UCzCWehBejA23yEz3zp7jlcg",
                       ChannelUploadsPlayListId = "UUzCWehBejA23yEz3zp7jlcg",
                       ChannelName = "Black Metal Promotion"
                   };
        }

        /// <summary>
        /// Test for GetFullVideoMetaDataOfChannelAsync
        /// </summary>
        [TestMethod]
        public void CheckThatSpecialVictimsChannel()
        {
            Channel channel = GetBMPChannel();
            var youtubeApi = SetUpTest(out Logger localLogger);

            var theTaskBaby = youtubeApi.GetFullVideoMetaDataOfChannelAsync(channel, 10);
            theTaskBaby.Wait();
            var listOfVideos = theTaskBaby.Result;
            Assert.AreEqual(listOfVideos.Count, 10);
        }

        /// <summary>
        /// Test for GetVideoMetaData.
        /// </summary>
        [TestMethod]
        public void CheckVideoDataOfAVideo()
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
        /// Check two channels and pick up 2 videos each.
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

            var folder1 = VideoMetaDataFull.GetChannelSubDir(WorkFolder, theTestChannel.ChannelId);
            var folder2 = VideoMetaDataFull.GetChannelSubDir(WorkFolder, secondChannel.ChannelId);
            Assert.AreEqual(Directory.GetFiles(folder1).Length, 2);
            Assert.AreEqual(Directory.GetFiles(folder2).Length, 2);
        }


        /// <summary>
        /// The test also checks the RollingFileUpdater functionality.
        /// </summary>
        [TestMethod]
        public void StartYoutubeWorkerAndUpdateRollingFileTest()
        {
            // Setup. The test is set up normally. However, you must ensure that channel directories already exist before the worker is started.
            // These channel directories contain any number of dummy video files. The test also checks the RollingFileUpdater functionality.
            var youtubeApi = SetUpTest(out var localLogger);
            void MyLocalCallback(string arg1, string message)
            {
                Assert.IsFalse(arg1 == ""); // Must not happen
                localLogger.LogDebug($"Callback was called first arg: {arg1}, second arg: {message}");
            }

            var theTestChannel = GetTestChannel();
            var secondChannel = GetTestChannel2();
            var channelList = new List<Channel> { theTestChannel, secondChannel };

            // Assert.AreEqual(Directory.GetFiles(WorkFolder).Length, 10);
            var ytManager = new YtManager(youtubeApi, WorkFolder);
            ytManager.CheckAndCreateChannelSubDirectories(channelList);

            FileHandlingTest.CreateVideoMetaFiles(10, theTestChannel.ChannelId, WorkFolder);
            FileHandlingTest.CreateVideoMetaFiles(10, secondChannel.ChannelId, WorkFolder);

            // Actual test. Each channel directory should now contain 10 dummy *.video files. The worker should fetch 2 real videos per channel
            // with the given parameters and then clean up the channel directories. Cleaning up means that each channel directory contains
            // only 150% of the number of videos fetched. Each channel directory may therefore contain 3 files. 
            var ddd = ytManager.StartYoutubeWorker(channelList, 2, MyLocalCallback);
            localLogger.LogDebug("Just started Youtube Worker. Now wait 10 Seconds.");
            ddd.Wait(TimeSpan.FromSeconds(7));
            localLogger.LogDebug("Stopped Youtube Worker and wait another 10 Seconds.");
            ytManager.StopYoutubeWorker();
            ddd.Wait(TimeSpan.FromSeconds(7));
            localLogger.LogDebug("Done very well. If theres no exception you're good.");

            var folder1 = VideoMetaDataFull.GetChannelSubDir(WorkFolder, theTestChannel.ChannelId);
            var folder2 = VideoMetaDataFull.GetChannelSubDir(WorkFolder, secondChannel.ChannelId);
            Assert.AreEqual(Directory.GetFiles(folder1).Length, 3);
            Assert.AreEqual(Directory.GetFiles(folder2).Length, 3);
        }

        /// <summary>
        /// This test writes 10 video files into the corresponding subfolder. Then 2 files are deleted from the subfolder.
        /// When the 'GetFullVideoMetaDataOfChannelAsync' method is called again, only 2 videos are returned.
        /// </summary>
        [TestMethod]
        public void GetListOfVideosWhenAlreadyFilesInSubfolder()
        {
            var youtubeApi = SetUpTest(out var localLogger);
            var victimChannel = GetVictimTestChannel();

            var theList = youtubeApi.GetFullVideoMetaDataOfChannelAsync(victimChannel, 10).Result;
            var subfolder = Path.Combine(WorkFolder, victimChannel.ChannelId);

            theList.ForEach(video =>
                            {
                                VideoMetaDataFull.SerializeToFileInSubfolder(video, WorkFolder);
                            });

            var fileNames = Directory.GetFiles(subfolder);
            File.Delete(fileNames[3]);
            File.Delete(fileNames[7]);

            theList = youtubeApi.GetFullVideoMetaDataOfChannelAsync(victimChannel, 10).Result;
            Assert.AreEqual(theList.Count, 2);
        }
    }
}