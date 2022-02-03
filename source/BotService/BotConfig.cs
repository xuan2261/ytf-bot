using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;
using SimpleLogger;
using TelegramApi;
using YoutubeApi;

namespace BotService
{
    public class Group
    {
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; }

        [JsonPropertyName("groupId")]
        public string GroupId { get; set; }
    }

    public class FaceBook
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("pw")]
        public string Pw { get; set; }

        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; }
    }


    public class BotConfig
    {
        [JsonPropertyName("faceBook")]
        public FaceBook FaceBook { get; set; }

        [JsonPropertyName("telegram")]
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
