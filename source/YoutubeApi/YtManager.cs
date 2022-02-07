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


        /// <remarks>
        /// My ass
        /// </remarks>
        /// <param name="youtubeApi">YoutubeApi</param>
        public YtManager(YoutubeApi youtubeApi)
        {
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
        /// Callback is called when successfully wrote a file and when errors appear. 2 input string, return
        /// void.
        /// </param>
        /// <param name="listOfExcludedVideos">Filter criterion. The videos in this list should not be included in the result.</param>
        /// <returns>The compiled list of videos is returned and written to a file.</returns>
        public async Task StartYoutubeWorker(List<Channel> channels,
                                             Action<string, string>? callback = null,
                                             List<VideoMetaDataSmall>? listOfExcludedVideos = null)
        {
            this.workerShallRun = true;
            while (this.workerShallRun)
            {
                var task = this.youtubeApi.CreateListWithFullVideoMetaDataAsync(channels, 10);

                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))) == task)
                {
                    if (task.Result.Count > 0)
                    {
                        var weReAtNowNowFileName = $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}_{VideoMetaDataFull.YoutubeSearchPattern}";
                        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, weReAtNowNowFileName);
                        await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(task.Result));
                        callback?.Invoke(weReAtNowNowFileName, "Created file successfully");
                    }
                    else
                    {
                        // Create no logs in this case because of myriads of log entries.
                        //this.logger.LogInfo("Found no new videos");
                    }
                }
                else
                {
                    var msg = "Timeout StartYoutubeWorker, cause of something. Do something. Don't just stand there, kill something!";
                    this.logger.LogError(msg);
                    callback?.Invoke("Error", msg);
                }

                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            callback?.Invoke("End", "YTManager stopped working. Press Return to end it all.");
        }
    }
}