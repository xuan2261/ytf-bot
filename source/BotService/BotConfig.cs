using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;
using SimpleLogger;
using TelegramApi;
using YoutubeApi;
using FacebookAutomation;

namespace BotService
{
    public class BotConfig
    {
        [JsonPropertyName("facebookConfig")]
        public FacebookConfig FacebookConfig { get; set; }

        [JsonPropertyName("telegramConfig")]
        public TelegramConfig TelegramConfig { get; set; }

        [JsonPropertyName("youtubeConfig")]
        public YoutubeConfig YoutubeConfig { get; set; }

        /// <summary>
        /// Read the application config from json file.
        /// </summary>
        /// <param name="pathToSjonFile"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static BotConfig LoadFromJsonFile(string pathToSjonFile, Logger? logger = null)
        {
            try
            {
                var result = JsonSerializer.Deserialize<BotConfig>(File.ReadAllText(pathToSjonFile));
                if (result != null)
                {
                    return result;
                }

                logger?.LogError($"Could not deserialize config file '{pathToSjonFile}'.");
                throw new SerializationException($"Could not deserialize config file '{pathToSjonFile}'.");

            }
            catch (Exception e)
            {
                logger?.LogError($"Error when reading json file '{pathToSjonFile}'. Exception: {e.Message}");
                throw;
            }
        }
    }
}
