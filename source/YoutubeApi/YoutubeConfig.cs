using System.Text.Json.Serialization;

namespace YoutubeApi;

public class Channel
{
    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }

    public Channel()
    {
        ChannelName = string.Empty;
        ChannelId = string.Empty;
    }
}

public class YoutubeConfig
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }

    [JsonPropertyName("channels")]
    public List<Channel> Channels { get; set; }

    public YoutubeConfig()
    {
        ApiKey = string.Empty;
        Channels = new List<Channel>();
    }
}