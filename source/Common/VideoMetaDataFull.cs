using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common
{
    public class VideoMetaDataFull
    {
        /// <summary>
        /// Search pattern and file extension for video files => "video"
        /// </summary>
        public static string VideoFileSearchPattern => "video";

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

        /// <summary>
        /// Converts plain string to base64 string.
        /// </summary>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Converts base64 string to plain string.
        /// </summary>
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Creates the full path with subfolder to file within videoMetaData based on the id of the Video.
        /// Subfolder is channel name.
        /// </summary>
        /// <param name="videoMetaData">File name</param>
        /// <param name="workingDirectory">Working directory</param>
        /// <returns>Full path to video meta data file based on the id of the video.</returns>
        public static string CreateFileNameWithSubFolder(VideoMetaDataFull videoMetaData, string workingDirectory)
        {
            var fileName = videoMetaData.Id + $".{VideoFileSearchPattern}";
            return Path.Combine(GetChannelSubDir(workingDirectory, videoMetaData.ChannelId), fileName);
        }

        /// <summary>
        /// Deserialize the list of videos inside the json file 'pathToJsonFile' into an object returned.
        /// </summary>
        /// <param name="pathToJsonFile">Self-explanatory</param>
        /// <returns>The list of deserialized videos.</returns>
        public static VideoMetaDataFull DeserializeFromFile(string pathToJsonFile)
        {
            var resultList = JsonSerializer.Deserialize<VideoMetaDataFull>(File.ReadAllText(pathToJsonFile));
            return resultList ?? new VideoMetaDataFull();
        }


        /// <summary>
        /// Creates a readable string with leading link to the video.
        /// </summary>
        /// <returns>Readable string</returns>
        public string GetReadableDescription()
        {
            var readableString = BuildYoutubeLinkToVideo() + Environment.NewLine + Environment.NewLine;
            readableString += Base64Decode(TitleBase64) + Environment.NewLine;
            readableString += PublishedAtRaw.ToString("yyyy-MM-ddTHH:mm:ssZ") + Environment.NewLine + Environment.NewLine;
            readableString += "Original Description:" + Environment.NewLine;
            readableString += Base64Decode(this.DescriptionBase64);
            return readableString;
        }

        /// <summary>
        /// Creates a clickable hyperlink out of self (current video).
        /// </summary>
        /// <returns>Clickable hyperlink</returns>
        public string BuildYoutubeLinkToVideo()
        {
            return "https://youtu.be/" + Id;
        }

        /// <summary>
        /// Check the description for the possibility of doing the post on a German channel. 
        /// </summary>
        /// <returns></returns>
        public bool IsGerman()
        {
            var videoDescription = Base64Decode(this.DescriptionBase64);

            if (videoDescription.Contains("german") |
                videoDescription.Contains("austria") |
                videoDescription.Contains("switzerland") |
                videoDescription.Contains("swiss"))
            {
                return true;
            }
            return false;
        }

        public static string GetChannelSubDir(string workDir, string channelId)
        {
            return Path.Combine(workDir, channelId);
        }
    }
}
