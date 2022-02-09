using System.Text.Json;
using Common;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SimpleLogger;

namespace YoutubeApi
{
    public class YoutubeApi
    {
        private static string TimeStampFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChannelTimeStamps");

        private string applicationName;
        private Dictionary<int, string> apiKeysDict;
        private int iterator;

        public Logger Logger { get; }

        /// <summary>
        /// Ctor.
        /// Initiates the YoutubeService.
        /// </summary>
        /// <param name="applicationName">Name of application (irrelevant)</param>
        /// <param name="apiKeys">Secret api key</param>
        /// <param name="theLogger">Logger if available</param>
        public YoutubeApi(string applicationName, List<string> apiKeys, Logger? theLogger = null)
        {
            this.Logger = theLogger ?? new Logger("YoutubeApi.log");
            try
            {
                if (!Directory.Exists(TimeStampFolder))
                {
                    Directory.CreateDirectory(TimeStampFolder);
                }

                this.apiKeysDict = new Dictionary<int, string>(apiKeys.Count);
                var localIterator = 0;
                apiKeys.ForEach(s =>
                                {
                                    this.apiKeysDict.Add(localIterator, s);
                                    localIterator++;
                                });

                this.applicationName = applicationName;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public YouTubeService GetYoutubeService()
        {
            var appName = this.applicationName + $"_{this.iterator:D2}";
            var service = new YouTubeService(new BaseClientService.Initializer()
                                             {
                                                 ApiKey = this.apiKeysDict[this.iterator],
                                                 ApplicationName = appName
            });
            this.iterator = (this.iterator + 1) % this.apiKeysDict.Count;
            Logger.LogDebug($"YoutubeServiceCreated with app name {appName}. Iterator was incremeted: {this.iterator}");
            return service;
        }

        /// <summary>
        /// This method returns a list within all meta data to videos found in the passed channel 'channel'.
        /// By means of the file "channelName.json", a time stamp is read out with the help of which it is determined whether there
        /// are new
        /// videos in the channel since the last read-out. After a successful procedure, the current timestamp is written to the
        /// file.
        /// </summary>
        /// <param name="channel">The Youtube channel.</param>
        /// <param name="maximumResult">Consider only this amount of results.</param>
        /// <param name="listOfExcludedVideos">The videos in that list will be excluded from the result. To be implemented:-(</param>
        /// <returns>The list of full meta data videos found in the channel.</returns>
        private async Task<List<VideoMetaDataFull>> GetFullVideoMetaDataOfChannelAsync(Channel channel,
                                                                                      int maximumResult,
                                                                                      List<VideoMetaDataSmall>? listOfExcludedVideos = null)
        {
            // The beginning of this perhaps time-consuming process needs to be secured so that videos that are published in
            // the meantime are not overlooked.
            var weAreAtNowNow = DateTime.UtcNow;
            var lastSuccessfulProcessZulu = GetLastSuccessfulCheckFromFile(channel, weAreAtNowNow);

            // This list contains all videos of the 'channel' including the complete 'Description'.
            var resultListOfChannelVideos = new List<VideoMetaDataFull>(maximumResult);
            try
            {
                var service = GetYoutubeService();
                var searchListRequest = service.Search.List("snippet");
                searchListRequest.ChannelId = channel.ChannelId;
                searchListRequest.Type = "video";
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.MaxResults = maximumResult;
                searchListRequest.PublishedAfter = lastSuccessfulProcessZulu.ToString("yyyy-MM-ddTHH:mm:ssZ");
               
                // The result of this call contains all the desired videos with all the information.
                // Unfortunately, only with an incomplete "Description". 
                var searchListResponse = await searchListRequest.ExecuteAsync();
                service.Dispose();

                if (searchListResponse.Items.Count > 0)
                {
                    // For each searchListResponse, perform another videoRequest. This means that for each video found, another request is made to obtain
                    // the complete "Description".
                    var tasks = searchListResponse.Items.Select(searchResults => GetVideoMetaData(searchResults.Id.VideoId)).ToArray();
                    Task.WaitAll(tasks);
                    var listOfVideoLists = tasks.Select(task => task.Result);

                    foreach (var videoList in listOfVideoLists)
                    {
                        resultListOfChannelVideos.AddRange(videoList);
                    }
                }
                this.Logger.LogDebug("YoutubeApi call was successful. Update TimeStamp in ChannelFiles");
                SetTimeStampWhenVideoCheckSuccessful(channel, weAreAtNowNow);
            }
            catch (Exception e)
            {
                this.Logger.LogError("Error, but go on." + Environment.NewLine + e.Message);
            }

            return resultListOfChannelVideos;
        }

        /// <summary>
        /// This method performs a VideoRequest for a single video to get the full description of the video.
        /// The confusing thing about this construct is that the result of this call is in a list.This is due to the YoutubeApi.
        /// </summary>
        /// <param name="videoId">The video to which the information is fetched.</param>
        /// <returns>This list contains exactly one video. At the moment I'm not sure how awesome it is:-(</returns>
        private async Task<List<VideoMetaDataFull>> GetVideoMetaData(string videoId)
        {
            var listOfChannelVideos = new List<VideoMetaDataFull>();

            try
            {
                var service = GetYoutubeService();
                var videoRequest = service.Videos.List("snippet");
                videoRequest.Id = videoId;
                var videoLisResponse = await videoRequest.ExecuteAsync();

                // This construct does not need to be parallelized because int items should only contain a single element.
                // And if not, so what? The loop is fast and contains no remote calls.
                foreach (var video in videoLisResponse.Items)
                {
                    var newVideo = new VideoMetaDataFull
                    {
                        Title = video.Snippet.Title,
                        TitleBase64 = VideoMetaDataFull.Base64Encode(video.Snippet.Title),
                        Id = video.Id,
                        ChannelId = video.Snippet.ChannelId,
                        ChannelTitle = video.Snippet.ChannelTitle,
                        DescriptionBase64 = VideoMetaDataFull.Base64Encode(video.Snippet.Description)
                    };
                    if (video.Snippet.PublishedAt != null)
                    {
                        newVideo.PublishedAtRaw = video.Snippet.PublishedAt.Value;
                    }

                    listOfChannelVideos.Add(newVideo);
                }
                service.Dispose();
            }
            catch (Exception e)
            {
                this.Logger.LogError("Error, but go on." + Environment.NewLine + e.Message);
            }
            return listOfChannelVideos;
        }


        /// <summary>
        /// Async Method to create a list with all the metadata of the videos contained in the channels in channelIds.
        /// </summary>
        /// <remarks>
        /// Note! To create the list of published videos, we only look at the videos that have been published since the last
        /// successful
        /// check. So this method reads the datetime of the lasst successful check for new videos from a file and returns it.
        /// If file does not exist, we're at now now! This means that you will most likely not get any results.
        /// </remarks>
        /// <param name="channelIds">List of Youtube channels.</param>
        /// <param name="maximumResult">Consider only this amount of results.</param>
        /// <param name="listOfExcludedVideos">The videos in that list will be excluded from the result.</param>
        /// <returns>List with videos if news available, empty list if not.</returns>
        public async Task<List<VideoMetaDataFull>> CreateListWithFullVideoMetaDataAsync(List<Channel> channelIds,
                                                                                        int maximumResult,
                                                                                        List<VideoMetaDataSmall>? listOfExcludedVideos = null)
        {
            List<VideoMetaDataFull> completeVideoList = new(maximumResult * channelIds.Count);
            await Task.Run(() =>
                           {
                               var tasks = channelIds
                                           .Select(channelId => GetFullVideoMetaDataOfChannelAsync(
                                                       channelId,
                                                       maximumResult,
                                                       listOfExcludedVideos)).ToArray();
                               Task.WaitAll(tasks);
                               var listOfVideoLists = tasks.Select(task => task.Result);

                               foreach (var videos in listOfVideoLists)
                               {
                                   completeVideoList.AddRange(videos);
                               }
                           });

            return completeVideoList;
        }

        /// <summary>
        /// To create the list of published videos, we only look at the videos that have been published since the last successful
        /// check. So this method reads the datetime of the lasst successful check for new videos from a file and returns it.
        /// If file does not exist, we're at now now! This means that you will most likely not get any results when looking for new videos.
        /// Note: Zulu time, no logging no exception handling
        /// </summary>
        public static DateTime GetLastSuccessfulCheckFromFile(Channel channel, DateTime nowNow)
        {
            var fullPathFileName = MakeChannelTimeStamp(channel.ChannelId);
            return File.Exists(fullPathFileName) ? JsonSerializer.Deserialize<DateTime>(File.ReadAllText(fullPathFileName)) : nowNow;
        }

        /// <summary>
        /// Each time the list of new videos is successfully read and passed on, the timestamp in the file must be reset.
        /// Note: Zulu time, no logging, no exception handling
        /// </summary>
        public static void SetTimeStampWhenVideoCheckSuccessful(Channel channel, DateTime nowNow)
        {
            File.WriteAllText(MakeChannelTimeStamp(channel.ChannelId), JsonSerializer.Serialize(nowNow));
        }

        /// <summary>
        /// Creates the filename of the channel time stamp file.
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public static string MakeChannelTimeStamp(string channelId)
        {
            return Path.Combine(TimeStampFolder, $"{channelId}.json");
        }
    }
}