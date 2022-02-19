using Common;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        /// <summary>
        /// The YTManager uses the YoutubeApi to generate a list of VideoMetaDataFull objects. This list is written to files
        /// located in the
        /// WorkDir during the main task of the worker.
        /// </summary>
        public readonly string WorkDir;

        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;
        private bool workerShallRun;

        /// <summary>
        /// Amount of videos requested on youtube api per channel => 10
        /// </summary>
        private int MaxCountOfRequestedVideosPerChannel => 10;

        /// <summary>
        /// Minimum timeout time in seconds per api call => 5
        /// </summary>
        private int MinTimeoutForApiCallInSeconds => 5;

        /// <summary>
        /// </summary>
        /// <param name="youtubeApi"></param>
        /// <param name="workDir"></param>
        public YtManager(YoutubeApi youtubeApi, string workDir)
        {
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }

            this.WorkDir = workDir;

            this.logger = youtubeApi.Logger;
            this.youtubeApi = youtubeApi;
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopYoutubeWorker()
        {
            this.workerShallRun = false;
        }

        /// <summary>
        /// This worker 
        /// </summary>
        /// <param name="channels">Channels that are searched</param>
        /// <param name="callback"> Callback arg1 is 'Info' or 'Error'. Arg2 is the detailed message.
        /// </param>
        public async Task StartYoutubeWorker(List<Channel> channels,
                                             Action<string, string>? callback = null)
        {
            try
            {
                // Dynamic timeout in seconds for calling the main method.
                var timeOut = MinTimeoutForApiCallInSeconds + channels.Count * 2;

                await Task.Run(() =>
                               {
                                   this.logger.LogDebug("Youtube worker task has started.");
                                   this.workerShallRun = true;
                                   while (this.workerShallRun)
                                   {
                                       var theTask = this.youtubeApi.CreateListWithFullVideoMetaDataAsync(channels,
                                           MaxCountOfRequestedVideosPerChannel);

                                       if (theTask.Wait(TimeSpan.FromSeconds(timeOut)))
                                       {
                                           var listOfVideoMetaOfAllChannels = theTask.Result;
                                           if (listOfVideoMetaOfAllChannels.Count > 0)
                                           {
                                               listOfVideoMetaOfAllChannels.ForEach(videoMetaData =>
                                                                                    {
                                                                                        this.youtubeApi.CreateVideoMetaDataFileInWorkingDirectory(
                                                                                            videoMetaData);
                                                                                    });

                                               // Clean up working directory, avoids endless amount of video files in director. Maximum number of
                                               // files has an overhang of 50%.  50% is a sentimental value.
                                               var maximumNumberOfFiles = (int)(channels.Count * (MaxCountOfRequestedVideosPerChannel * 1.5));
                                               FileHandling.RollingFileUpdater(this.WorkDir,
                                                                               VideoMetaDataFull.VideoFileSearchPattern,
                                                                               maximumNumberOfFiles);

                                               // Log all videos of all channels
                                               var message = YoutubeApi.CreateMessageWithVideosOfAllChannels(listOfVideoMetaOfAllChannels);
                                               callback?.Invoke("INFO  ***", message);
                                               this.logger.LogInfo(message);
                                           }
                                           else
                                           {
                                               callback?.Invoke("INFO  ***", "Found no new videos.");
                                               this.logger.LogInfo("Found no new videos.");
                                           }
                                       }
                                       else
                                       {
                                           var msg =
                                               "Timeout StartYoutubeWorker, cause of something. Do something. Don't just stand there, kill something!";
                                           this.logger.LogError(msg);
                                           callback?.Invoke("ERROR ***", msg);
                                       }

                                       Thread.Sleep(GetSleepTime());
                                   }

                                   callback?.Invoke("END   ***", "YTManager stopped working. Press Return to end it all.");
                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                callback?.Invoke("ERROR ***", "In Worker: " + e.Message);
            }

            this.logger.LogDebug("Youtube worker ended correctly. Loop and Task has ended. Method is exited.");
        }

        /// <summary>
        /// The method controls the frequency of the Api calls. Most channels stick to certain times to publish videos. Comparable to
        /// prime time on TV. This construct is intended to ensure that fewer api calls take place at night and that
        /// new videos are searched for more often during prime time.
        /// </summary>
        /// <returns>Dynamic sleep time depending on the time of day.</returns>
        public TimeSpan GetSleepTime()
        {
            return TimeSpan.FromMinutes(10);
        }
    }
}