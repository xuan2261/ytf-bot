
using System.Text.Json;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;
        private readonly string dateTimeFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "youtubeStartUp.json");
        private DateTime lastCheckSuccessful;


        /// <remarks>
        /// My ass</remarks>
        /// <param name="apiKey">Secret api key to access youtube api.</param>
        /// <param name="theLogger">The logger if exists</param>
        public YtManager(string apiKey, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger("YoutubeApi.log");
            //GetFullVideoMetaData();
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


        public List<VideoMetaDataFull> GetFullVideoMetaData(List<string> channelIds, List<VideoMetaDataSmall> listOfExcludedVideos)
        {
            var listOfFullVideoMetaData = new List<VideoMetaDataFull>();

            // nts. Diedes Liste sollte die Videos enthalten, die ausgeschlossen werden --> Filterkriterium
            // Der Zeitstempel dieser App sollte das zweite Filterkriterium sein und zwar nicht ab Start, sondern immer dann gesetzt wenn Arbeit verrichtet wurde.
            var detectedVideos = new List<VideoMetaDataSmall>();
            for (int i = 0; i < 5; i++)
            {
                detectedVideos.Add(new VideoMetaDataSmall
                {
                    Id = i.ToString(),
                    DetectedAt = DateTime.UtcNow,
                    Title = $"{i:000}"
                });
            }

            File.WriteAllText("aaaa.json", JsonSerializer.Serialize(detectedVideos));
            return listOfFullVideoMetaData;
        }

        /// <summary>
        /// To create the list of published videos, we only look at the videos that have been published since the last successful check.
        /// This method reads the datetime of the lasst successful check for new videos from a file and stores it into 'this.lastCheckSuccessful'.
        /// </summary>
        private void GetLastSuccessfulCheckFromFile()
        {
            this.lastCheckSuccessful = File.Exists(this.dateTimeFile) ? JsonSerializer.Deserialize<DateTime>(File.ReadAllText(this.dateTimeFile)) : DateTime.UtcNow;
        }

        /// <summary>
        /// Each time the list of new videos is successfully read and passed on, the timestamp in the file must be reset.
        /// </summary>
        private void SetTimeStampWhenVideoCheckSuccessful()
        {
            this.lastCheckSuccessful = DateTime.UtcNow;
            File.WriteAllText(this.dateTimeFile, JsonSerializer.Serialize(this.lastCheckSuccessful));
            this.logger.LogInfo($"Created new youtubeStartup.json at {this.lastCheckSuccessful}");
        }
    }
}
