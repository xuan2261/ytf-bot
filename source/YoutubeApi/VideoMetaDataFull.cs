using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoutubeApi
{
    public class VideoMetaDataFull
    {
        private string description;
        private string title;

        public VideoMetaDataFull()
        {
            this.title = string.Empty;
            Id = string.Empty;
            PublishedAtRaw = DateTime.MinValue;
            ChannelId = string.Empty;
            ChannelTitle = string.Empty;
            this.description = string.Empty;
        }

        [JsonPropertyName("title")]
        public string Title
        {
            get => Base64Decode(this.title);
            set => this.title = Base64Encode(value);
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("publishedAtRaw")]
        public DateTime PublishedAtRaw { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("channelTitle")]
        public string ChannelTitle { get; set; }

        [JsonPropertyName("description")]
        public string Description
        {
            get => Base64Decode(this.description);
            set => this.description = Base64Encode(value);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static void Serialize(List<VideoMetaDataFull> videosToSerialize)
        {
            var json = JsonSerializer.Serialize(videosToSerialize);
            File.WriteAllText(@"youtubeVideos.json", json);
        }

        public static List<VideoMetaDataFull>? Deserialize(string pathToJsonFile)
        {
            return JsonSerializer.Deserialize<List<VideoMetaDataFull>>(File.ReadAllText(pathToJsonFile));
        }
    }
}
