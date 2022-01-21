using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;
using SimpleLogger;

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

    public class Bot
    {
        [JsonPropertyName("botName")]
        public string BotName { get; set; }

        [JsonPropertyName("botToken")]
        public string BotToken { get; set; }
    }

    public class Chat
    {
        [JsonPropertyName("chatName")]
        public string ChatName { get; set; }

        [JsonPropertyName("chatId")]
        public long ChatId { get; set; }
    }

    public class Telegram
    {
        [JsonPropertyName("bots")]
        public List<Bot> Bots { get; set; }

        [JsonPropertyName("chats")]
        public List<Chat> Chats { get; set; }
    }

    public class Channel
    {
        [JsonPropertyName("channelName")]
        public string ChannelName { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("lastSuccessfulFindZulu")]
        public DateTime? LastSuccessfulFindZulu { get; set; }
    }

    public class Youtube
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }

        [JsonPropertyName("channels")]
        public List<Channel> Channels { get; set; }
    }

    public class BotConfig
    {
        [JsonPropertyName("faceBook")]
        public FaceBook FaceBook { get; set; }

        [JsonPropertyName("telegram")]
        public Telegram Telegram { get; set; }

        [JsonPropertyName("youtube")]
        public Youtube Youtube { get; set; }

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
