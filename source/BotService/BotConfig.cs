using System.Runtime.Serialization;
using Newtonsoft.Json;
using SimpleLogger;

namespace BotService
{
    public class Group
    {
        [JsonProperty("groupName")]
        public string GroupName { get; set; }

        [JsonProperty("groupId")]
        public string GroupId { get; set; }
    }

    public class FaceBook
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("pw")]
        public string Pw { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }
    }

    public class Bot
    {
        [JsonProperty("botName")]
        public string BotName { get; set; }

        [JsonProperty("botToken")]
        public string BotToken { get; set; }
    }

    public class Chat
    {
        [JsonProperty("chatName")]
        public string ChatName { get; set; }

        [JsonProperty("chatId")]
        public long ChatId { get; set; }
    }

    public class Telegram
    {
        [JsonProperty("bots")]
        public List<Bot> Bots { get; set; }

        [JsonProperty("chats")]
        public List<Chat> Chats { get; set; }
    }

    public class Channel
    {
        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        [JsonProperty("channelId")]
        public string ChannelId { get; set; }
    }

    public class Youtube
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty("channels")]
        public List<Channel> Channels { get; set; }
    }

    public class BotConfig
    {
        [JsonProperty("faceBook")]
        public FaceBook FaceBook { get; set; }

        [JsonProperty("telegram")]
        public Telegram Telegram { get; set; }

        [JsonProperty("youtube")]
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
                var result = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(pathToSjonFile));
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
