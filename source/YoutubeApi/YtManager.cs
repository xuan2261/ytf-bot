
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
        /// <param name="youtubeApi">YoutubeApi</param>
        /// <param name="theLogger">The logger if exists</param>
        public YtManager(YoutubeApi youtubeApi)
        {
            this.logger = youtubeApi.Logger;
            this.youtubeApi = youtubeApi;
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

            var task = this.youtubeApi.CreateListWithFullVideoMetaDataAsync(channels, 10);
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
