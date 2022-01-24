
using System.Text.Json;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;


        /// <remarks>
        /// My ass</remarks>
        /// <param name="apiKey">Secret api key to access youtube api.</param>
        /// <param name="theLogger">The logger if exists</param>
        public YtManager(string apiKey, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger("YoutubeApi.log");
            try
            {
                this.youtubeApi = new YoutubeApi("YoutubeApi", apiKey, this.logger);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// This method returns a list within all metadata of videos that were published in the channels in 'channelIds'.
        /// In addition, the result is written to a Json file.
        /// </summary>
        /// <param name="channels">Channels that are searched</param>
        /// <param name="listOfExcludedVideos">Filter criterion. The videos in this list should not be included in the result.</param>
        /// <returns>The compiled list of videos is returned and written to a file.</returns>
        public void StartYoutubeWorkerWorker(List<Channel> channels, List<VideoMetaDataSmall>? listOfExcludedVideos = null)
        {
            var listOfFullVideoMetaData = new List<VideoMetaDataFull>();

            var task = this.youtubeApi.CreateVideoFileAsync(channels, 10);
            if (!task.Wait(TimeSpan.FromSeconds(10)))
            {
                this.logger.LogError($"Timeout, cause of something. Do something.");
            }
            else
            {
                var list = task.Result;
            }
           

            File.WriteAllText("aaaa.json", JsonSerializer.Serialize(listOfFullVideoMetaData));
        }

    }
}
