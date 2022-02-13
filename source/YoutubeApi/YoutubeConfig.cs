using System.Text.Json.Serialization;

namespace YoutubeApi;

public class Channel
{
    [JsonPropertyName("channelName")]
    public string ChannelName { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }

    [JsonPropertyName("channelUploadsPlayListId")]
    public string ChannelUploadsPlayListId { get; set; }

    public Channel()
    {
        ChannelName = string.Empty;
        ChannelId = string.Empty;
        ChannelUploadsPlayListId = string.Empty;
    }
}

public class YoutubeConfig
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }

    [JsonPropertyName("apiKey4Testing")]
    public string ApiKey4Testing { get; set; }

    [JsonPropertyName("channels")]
    public List<Channel> Channels { get; set; }

    public YoutubeConfig()
    {
        ApiKey = string.Empty;
        ApiKey4Testing = string.Empty;
        Channels = new List<Channel>();
    }
}