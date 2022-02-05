using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common
{
    public class VideoMetaDataFull
    {
        public VideoMetaDataFull()
        {
            Title = string.Empty;
            TitleBase64 = string.Empty;
            DescriptionBase64 = string.Empty;
            Id = string.Empty;
            PublishedAtRaw = DateTime.MinValue;
            ChannelId = string.Empty;
            ChannelTitle = string.Empty;
        }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("titleBase64")]
        public string TitleBase64 { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("publishedAtRaw")]
        public DateTime PublishedAtRaw { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("channelTitle")]
        public string ChannelTitle { get; set; }

        [JsonPropertyName("descriptionBase64")]
        public string DescriptionBase64 { get; set; }


        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Serialize 'videosToSerialize into file 'youtubeVideos.json'.
        /// </summary>
        /// <param name="videosToSerialize">List of videos to serialize.</param>
        public static void SerializeIntoFile(List<VideoMetaDataFull> videosToSerialize)
        {
            var json = JsonSerializer.Serialize(videosToSerialize);
            File.WriteAllText(@"youtubeVideos.json", json);
        }

        /// <summary>
        /// Deserialize the list of videos inside the json file 'pathToJsonFile' into an object returned.
        /// </summary>
        /// <param name="pathToJsonFile">Self-explanatory</param>
        /// <returns>The list of deserialized videos.</returns>
        public static List<VideoMetaDataFull> DeserializeFromFile(string pathToJsonFile)
        {
            var resultList = JsonSerializer.Deserialize<List<VideoMetaDataFull>>(File.ReadAllText(pathToJsonFile));
            return resultList ?? new List<VideoMetaDataFull>();
        }


        // Hier Saich Boy. Der Hyperlink fehlt und der Test läuft noch schief.
        public string GetReadableDescription()
        {
            return Base64Decode(DescriptionBase64);
        }
    }
}
