using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeApi
{
    internal class YtVideos
    {
        public List<Video> videos { get; set; }
    }

    public class Video
    {
        public string title { get; set; }
        public string id { get; set; }
        public DateTime PublishedArRaw { get; set; }
        public string ChannelId { get; set; }
        public string ChannelTitle { get; set; }
        public string Description { get; set; }
    }
}
