using System.Text.Json.Serialization;

namespace TelegramApi
{
    public class Bot
    {
        [JsonPropertyName("botName")]
        public string BotName { get; set; }

        [JsonPropertyName("botToken")]
        public string BotToken { get; set; }

        public Bot()
        {
            BotName = string.Empty;
            BotToken = string.Empty;
        }
    }

    public class Chat
    {
        [JsonPropertyName("chatName")]
        public string ChatName { get; set; }

        [JsonPropertyName("chatId")]
        public long ChatId { get; set; }

        public Chat()
        {
            ChatName = string.Empty;
        }
    }

    public class TelegramConfig
    {
        [JsonPropertyName("bots")]
        public List<Bot> Bots { get; set; }

        [JsonPropertyName("chats")]
        public List<Chat> Chats { get; set; }

        public TelegramConfig()
        {
            Bots = new List<Bot>();
            Chats = new List<Chat>();
        }
    }
}
