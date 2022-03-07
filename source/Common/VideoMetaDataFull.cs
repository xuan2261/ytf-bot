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
        /// Deserialize the list of videos inside the json file 'pathToVideoFile' into an object returned.
        /// </summary>
        /// <param name="pathToVideoFile">Self-explanatory</param>
        /// <returns>The list of deserialized videos.</returns>
        public static VideoMetaDataFull DeserializeFromFile(string pathToVideoFile)
        {
            var resultList = JsonSerializer.Deserialize<VideoMetaDataFull>(File.ReadAllText(pathToVideoFile));
            return resultList ?? new VideoMetaDataFull();
        }

        /// <summary>
        /// Creates a video meta data file in the channel subfolder of the working directory. File name based on the Id of the video.
        /// Subdirectory is created if it does not already exist. 
        /// </summary>
        /// <param name="videoMetaData">Video meta data within in channelId for subfolder</param>
        /// <param name="workingDirectory">Working directory in which the subdirectories are contained.</param>
        public static void SerializeToFileInSubfolder(VideoMetaDataFull videoMetaData, string workingDirectory)
        {
            var fileName = videoMetaData.Id + $".{VideoFileSearchPattern}";
            var subfolderFullPath = GetChannelSubDir(workingDirectory, videoMetaData.ChannelId);
            var videoFileFullPath = Path.Combine(subfolderFullPath, fileName);

            if (!Directory.Exists(subfolderFullPath))
            {
                Directory.CreateDirectory(subfolderFullPath);
            }

            File.WriteAllText(videoFileFullPath, JsonSerializer.Serialize(videoMetaData));
        }

        /// <summary>
        /// Deserializes all files in 'notYetProcessedFiles' and returns a list of VideoMetaDataFull.
        /// </summary>
        /// <param name="notYetProcessedFiles">List of files that contains full paths to video files.</param>
        /// <param name="filesNotFound">Output parameter that contains the files that were not found.</param>
        /// <returns>The list of VideoMetaData</returns>
        public static  List<VideoMetaDataFull> DeserializeFiles(List<string> notYetProcessedFiles, out List<string> filesNotFound)
        {
            List<VideoMetaDataFull> listOfMetaVideoDate = new List<VideoMetaDataFull>();
            var notFound = new List<string>();
            notYetProcessedFiles.ForEach(file =>
                                         {
                                             if (File.Exists(file))
                                             {
                                                 listOfMetaVideoDate.Add(DeserializeFromFile(file));
                                             }
                                             else
                                             {
                                                 notFound.Add(file);
                                             }
                                         });
            filesNotFound = notFound;
            return listOfMetaVideoDate;
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
            readableString += "Original Description via " + ChannelTitle + ":" + Environment.NewLine;
            readableString += Base64Decode(this.DescriptionBase64);
            return readableString;
        }

        /// <summary>
        /// Creates a short video description for facebook posts.
        /// That's because fb are idiots and group postings aren't allowed anymore for random apps.
        /// </summary>
        /// <returns></returns>
        public string GetFacebookDescription()
        {
            var readableString = BuildYoutubeLinkToVideo() + Environment.NewLine + Environment.NewLine;
            readableString += Base64Decode(TitleBase64) + Environment.NewLine;

            try
            {
                List<string> lines = new List<string>(Base64Decode(this.DescriptionBase64).Split(new string[] { "\n" },
                                                                                                 StringSplitOptions.RemoveEmptyEntries));
                var linesToShow = lines.Where(line => line.Contains("Genre") | line.Contains("Country")).Take(2);
                foreach (var item in linesToShow)
                {
                    readableString += item + Environment.NewLine;
                }
            }
            catch
            {
                // no logging, nothing here, bad luck
                // description is well enough, if this fails, so don't mind.
            }

            readableString += Environment.NewLine;
            readableString += PublishedAtRaw.ToString("yyyy-MM-ddTHH:mm:ssZ") + $" via {ChannelTitle}" + Environment.NewLine + Environment.NewLine + Environment.NewLine;

            // Promo own shit
            readableString += "t.me/blackmetalpromotion" + Environment.NewLine;
            readableString += "t.me/germanblackmetal" + Environment.NewLine;
           
            // Add the link to the video a second time. That's because... dont know where to start fb fuck off.
            readableString += BuildYoutubeLinkToVideo();

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

            if (videoDescription.Contains("German") |
                videoDescription.Contains("german") |
                videoDescription.Contains("Austria") |
                videoDescription.Contains("austria") |
                videoDescription.Contains("Switzerland") |
                videoDescription.Contains("Swiss") |
                videoDescription.Contains("swiss"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns full path to a channels subfolder in the working directory.
        /// Example: C:\The_Working_Dir\ChannelId\
        /// </summary>
        /// <param name="workDir"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public static string GetChannelSubDir(string workDir, string channelId)
        {
            return Path.Combine(workDir, channelId);
        }
    }
}
