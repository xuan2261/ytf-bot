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
        /// Minimum timeout time in seconds per api call => 5
        /// </summary>
        private int MinTimeoutForApiCallInSeconds => 5;

        /// <summary>
        /// Ctor.
        /// Creates the working directory if it does not already exist.
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
        /// 
        /// </summary>
        /// <param name="channels">Channels to search videos in</param>
        /// <param name="maxResultsPerChannel">Maximum of results per channel and api list request</param>
        /// <param name="callback">Arg1: info and arg2: message.</param>
        /// <returns></returns>
        public async Task StartYoutubeWorker(List<Channel> channels,
                                             int maxResultsPerChannel,
                                             Action<string, string>? callback = null)
        {
            try
            {
                CheckAndCreateChannelSubDirectories(channels);

                // Dynamic timeout in seconds for calling the main method.
                var timeOut = MinTimeoutForApiCallInSeconds + channels.Count * 2;

                await Task.Run(() =>
                               {
                                   this.logger.LogDebug("Youtube worker task has started.");
                                   this.workerShallRun = true;
                                   while (this.workerShallRun)
                                   {
                                       var theTask = this.youtubeApi.CreateListWithFullVideoMetaDataAsync(channels,
                                           maxResultsPerChannel);

                                       if (theTask.Wait(TimeSpan.FromSeconds(timeOut)))
                                       {
                                           var listOfVideoMetaOfAllChannels = theTask.Result;
                                           if (listOfVideoMetaOfAllChannels.Count > 0)
                                           {
                                               listOfVideoMetaOfAllChannels.ForEach(videoMetaData =>
                                                                                    {
                                                                                        VideoMetaDataFull.SerializeToFileInSubfolder(
                                                                                            videoMetaData,
                                                                                            this.WorkDir);
                                                                                    });
                                               
                                               // Avoid unlimited files in directories 
                                               CleanUpWorkingDirectories(channels, maxResultsPerChannel); 
                                               
                                               // Log all videos of all channels
                                               var message = YoutubeApi.CreateMessageWithVideoDataMetaInformation(listOfVideoMetaOfAllChannels);
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
        /// Clean up working directories, avoids endless amount of video files in directories. Maximum number of files has an overhang of 200%.
        /// 200% is a sentimental value.
        /// </summary>
        /// <param name="maxResultsPerChannel">Maximum of results per channel and api list request></param>;
        /// <param name="channels">All channels to be cleaned up.</param>
        private void CleanUpWorkingDirectories(List<Channel> channels, int maxResultsPerChannel)
        {
            var maximumNumberOfFiles = (int)(maxResultsPerChannel * 2);
            channels.ForEach(channel =>
                             {
                                 FileHandling.RollingFileUpdater(VideoMetaDataFull.GetChannelSubDir(this.WorkDir, channel.ChannelId),
                                                                 VideoMetaDataFull.VideoFileSearchPattern,
                                                                 maximumNumberOfFiles);
                             });
        }

        /// <summary>
        /// Create channel sub directories, because the channel videos are stored in subfolders per channel.
        /// </summary>
        /// <param name="channels">All channels to create a subfolder for.</param>
        public void CheckAndCreateChannelSubDirectories(List<Channel> channels)
        {
            channels.ForEach(channel =>
                             {
                                 var directoryName = VideoMetaDataFull.GetChannelSubDir(this.WorkDir, channel.ChannelId);
                                 if (!Directory.Exists(directoryName))
                                 {
                                     Directory.CreateDirectory(directoryName);
                                 }
                             });
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