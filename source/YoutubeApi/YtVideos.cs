using System.Text;
using Newtonsoft.Json;

namespace YoutubeApi
{
    public class YtVideos
    {
        [JsonProperty("videos")]
        public List<Video> Videos { get; set; }

        public YtVideos(int capacity=10)
        {
            this.Videos = new List<Video>(capacity);
        }

        public static void SerializeObject(YtVideos videosToSerialize)
        {
            var json = JsonConvert.SerializeObject(videosToSerialize);
            File.WriteAllText(@"youtubeVideos.json", json);
        }
    }

    public class Video
    {
        private string description;

        public Video()
        {
            Title = string.Empty;
            Id = string.Empty;
            PublishedAtRaw = DateTime.MinValue;
            ChannelId = string.Empty;
            ChannelTitle = string.Empty;
            this.description = string.Empty;
        }

        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("publishedAtRaw")]
        public DateTime PublishedAtRaw { get; set; }

        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        [JsonProperty("channelTitle")]
        public string ChannelTitle { get; set; }

        [JsonProperty("description")]
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
    }
}
