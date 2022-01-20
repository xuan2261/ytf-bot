
using System.Text.Json;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;
        private readonly string dateTimeFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "youtubeStartUp.json");
        private DateTime lastCheckSuccessfulZulu;


        /// <remarks>
        /// My ass</remarks>
        /// <param name="apiKey">Secret api key to access youtube api.</param>
        /// <param name="theLogger">The logger if exists</param>
        public YtManager(string apiKey, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger("YoutubeApi.log");
            try
            {
                GetLastSuccessfulCheckFromFile();
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
        /// <param name="channelIds">Channels that are searched</param>
        /// <param name="listOfExcludedVideos">Filter criterion. The videos in this list should not be included in the result.</param>
        /// <returns>The compiled list of videos is returned and written to a file.</returns>
        public void StartFullVideoMetaDataWorker(List<string> channelIds, List<VideoMetaDataSmall>? listOfExcludedVideos = null)
        {
            var listOfFullVideoMetaData = new List<VideoMetaDataFull>();

           this.youtubeApi.CreateVideoFile(channelIds, this.lastCheckSuccessfulZulu, 10);
           

            File.WriteAllText("aaaa.json", JsonSerializer.Serialize(listOfFullVideoMetaData));
        }

        /// <summary>
        /// To create the list of published videos, we only look at the videos that have been published since the last successful check.
        /// This method reads the datetime of the lasst successful check for new videos from a file and stores it into 'this.lastCheckSuccessfulZulu'.
        /// Note: Zulu time.
        /// </summary>
        private void GetLastSuccessfulCheckFromFile()
        {
            this.lastCheckSuccessfulZulu = File.Exists(this.dateTimeFile) ? JsonSerializer.Deserialize<DateTime>(File.ReadAllText(this.dateTimeFile)) : DateTime.UtcNow;
        }

        /// <summary>
        /// Each time the list of new videos is successfully read and passed on, the timestamp in the file must be reset.
        /// Note: Zulu time.
        /// </summary>
        private void SetTimeStampWhenVideoCheckSuccessful()
        {
            this.lastCheckSuccessfulZulu = DateTime.UtcNow;
            File.WriteAllText(this.dateTimeFile, JsonSerializer.Serialize(this.lastCheckSuccessfulZulu));
            this.logger.LogInfo($"Created new youtubeStartup.json at {this.lastCheckSuccessfulZulu}");
        }
    }
}
