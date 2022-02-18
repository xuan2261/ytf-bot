using System.Text.Json;
using Common;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;
        private bool workerShallRun;
        private readonly int maxCountOfRequestedVideosPerChannel = 10;
        private readonly int minTimeoutForApiCallInSeconds = 5;

        /// <summary>
        /// The YTManager uses the YoutubeApi to generate a list of VideoMetaDataFull objects. This list is written to files
        /// located in the
        /// WorkDir during the main task of the worker.
        /// </summary>
        public readonly string WorkDir;

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
        /// This method returns a list within all metadata of videos that were published in the channels in 'channelIds'.
        /// In addition, the result is written to a Json file.
        /// </summary>
        /// <param name="channels">Channels that are searched</param>
        /// <param name="callback">
        /// Callback is called when
        /// - successfully wrote a file then arg1: fileName, arg2: Created "file successfully".
        /// - an error occurs then arg1: "Error", arg2: message
        /// - regular end of worker then arg1: "End", arg2: message.
        /// </param>
        /// <returns>The compiled list of videos is returned and written to a file.</returns>
        public async Task StartYoutubeWorker(List<Channel> channels,
                                             Action<string, string>? callback = null)
        {
            try
            {
                // Dynamic timeout in seconds for calling the main method.
                var timeOut = this.minTimeoutForApiCallInSeconds + channels.Count * 2;

                await Task.Run(() =>
                               {
                                   this.logger.LogDebug("Youtube worker task has started.");
                                   this.workerShallRun = true;
                                   while (this.workerShallRun)
                                   {
                                       var theTask = this.youtubeApi.CreateListWithFullVideoMetaDataAsync(channels,
                                                                                                          this.maxCountOfRequestedVideosPerChannel);

                                       if (theTask.Wait(TimeSpan.FromSeconds(timeOut)))
                                       {
                                           var listOfVideoMetaOfAllChannels = theTask.Result;
                                           if (listOfVideoMetaOfAllChannels.Count > 0)
                                           {

                                               listOfVideoMetaOfAllChannels.ForEach(videoMetaData =>
                                               {
                                                   this.youtubeApi.CreateVideoMetaDataFileInWorkingDirectory(videoMetaData);
                                               });


                                               // Dann Aufräumen. Bidde auch dynamisch --> je mehr Channels desto mehr Dateien dürfen existieren.
                                               var maximumOfFiles = 100; // TODO noch überlegen
                                               FileHandling.RollingFileUpdater(this.WorkDir, VideoMetaDataFull.VideoFileSearchPattern, );

                                               //var fullPathYoutubeVideoMetaFile = Path.Combine(this.WorkDir, weReAtNowNowFileName);
                                               //File.WriteAllText(fullPathYoutubeVideoMetaFile, JsonSerializer.Serialize(listOfVideoMetaFiles));


                                               //// Log all videos of all channels
                                               //var message = YoutubeApi.CreateMessageWithVideosOfAllChannels(listOfVideoMetaFiles);
                                               //callback?.Invoke("INFO  ***",
                                               //                 $"File {message} created with videos of all channels. {message}");
                                               //this.logger.LogInfo($"File {message} created with videos of all channels. {message}");






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

                                       // This call guarantees that there are never more or always exactly 50 files of the type VideoMetaDateFull.
                                       FileHandling.RollingFileUpdater(this.WorkDir, VideoMetaDataFull.YoutubeSearchPattern, 50);
                                       Thread.Sleep(TimeSpan.FromMinutes(10));
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
    }
}