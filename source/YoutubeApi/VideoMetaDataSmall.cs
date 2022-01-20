
using System.Text.Json.Serialization;

namespace YoutubeApi
{
    public class VideoMetaDataSmall
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("detectedAt")]
        public DateTime DetectedAt { get; set; }

        public VideoMetaDataSmall()
        {
            Title = string.Empty;
            Id = string.Empty;
            DetectedAt = DateTime.MinValue;
        }
    }
}
