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
        /// Returns the test channel and instantiates a youtube api.
        /// </summary>
        /// <remarks>
        /// Important! Before anything can be tested here, it must be ensured that there is a final successful check for published
        /// videos of a channel. This means that this method also creates a file with a timestamp that is far in the past. In the
        /// channel used here, all tests should return all videos of the channel.
        /// </remarks>
        /// <param name="youtubeApi">The instantiated youtube Api</param>
        /// <returns></returns>
        private static Channel SetUpTest(out YoutubeApi.YoutubeApi youtubeApi, out Logger localLogger)
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            Channel theTestChannel = new Channel
            {
                ChannelId = "UCOCZKlOz6cNs2qiIhls5cqQ",
                ChannelName = "Njal"
            };
            localLogger = new Logger("yt_test.log");
            localLogger.LogDebug("Test was set up.");
            youtubeApi = new YoutubeApi.YoutubeApi("TestApp", botConfig.YoutubeConfig.ApiKeys, localLogger);
            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(theTestChannel, new DateTime(2021, 1, 1));
            return theTestChannel;
        }

        [TestMethod]
        public void TestGetYoutubeService()
        {
            var localLogger = new Logger("yt_test.log");
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            var youtubeApi = new YoutubeApi.YoutubeApi("TestApp", botConfig.YoutubeConfig.ApiKeys.GetRange(1, 3), localLogger);
            for (int i = 0; i < 50; i++)
            {
                var service = youtubeApi.GetYoutubeService();
                service.Dispose();
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void CheckTimeStamp()
        {
            var timeStamp = "2022-02-10T02:42:49Z";

            var dateTime = DateTime.ParseExact(timeStamp,
                                               "yyyy-MM-ddTHH:mm:ssZ",
                                               System.Globalization.CultureInfo.InvariantCulture);

            var shit = DateTimeOffset.Parse(timeStamp).UtcDateTime;
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
            string thefile = string.Empty;
            var theTestChannel = SetUpTest(out var youtubeApi, out var localLogger);
            var secondChannel = new Channel
            {
                ChannelId = "UCraxywJxOEv-zQ2Yvmp4LtA",
                ChannelName = "Njals Traum - Thema"
            };
            YoutubeApi.YoutubeApi.SetTimeStampWhenVideoCheckSuccessful(secondChannel, new DateTime(2021, 1, 1));

            void MyLocalCallback(string file, string message)
            {
                Assert.IsFalse(file == ""); // Must not happen
                if (file != "End")
                {
                    thefile = file;
                }

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

            // the file was set in the callback. This works in this case, because there will be only one file.
            Assert.IsTrue(File.Exists(Path.Combine(WorkFolder, thefile)));
        }
    }
}