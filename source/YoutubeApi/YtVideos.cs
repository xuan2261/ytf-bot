using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }

    public class Video
    {
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
        public string Description { get; set; }
    }
}
