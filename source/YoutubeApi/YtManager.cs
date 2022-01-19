
using System.Text.Json;
using SimpleLogger;

namespace YoutubeApi
{
    public class YtManager
    {
        private readonly YoutubeApi youtubeApi;
        private readonly Logger logger;
        private readonly string dateTimeFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "youtubeStartUp.json");
        private DateTime firsStartOfYoutubeAt;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="apiKey">Secret api key to access youtube api.</param>
        /// <param name="theLogger">The logger if exists</param>
        public YtManager(string apiKey, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger("YoutubeApi.log");
            GetVideos();
            try
            {
                CheckStoredDateTime();
                this.youtubeApi = new YoutubeApi("YoutubeApi", apiKey, this.logger);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }

        public void GetVideos()
        {

            var detectedVideos = new List<DetectedVideos>();
            for (int i = 0; i < 5; i++)
            {
                detectedVideos.Add(new DetectedVideos
                {
                    Id = i.ToString(),
                    DetectedAt = DateTime.UtcNow,
                    Title = $"{i:000}"
                });
            }

            File.WriteAllText("aaaa.json",JsonSerializer.Serialize(detectedVideos));
        }

        /// <summary>
        /// To create the list of published videos, we only look at the videos that have been published since the first launch of this application.
        /// This method writes the date of the start-up to a file or reads it from a file.
        /// </summary>
        private void CheckStoredDateTime()
        {
            if (File.Exists(this.dateTimeFile))
            {
                firsStartOfYoutubeAt = JsonSerializer.Deserialize<DateTime>(File.ReadAllText(this.dateTimeFile));
            }
            else
            {
                firsStartOfYoutubeAt = DateTime.UtcNow;
                File.WriteAllText(this.dateTimeFile, JsonSerializer.Serialize(firsStartOfYoutubeAt));
                this.logger.LogInfo($"Created new youtubeStartup.json at {firsStartOfYoutubeAt}");
            }
        }
    }
}
