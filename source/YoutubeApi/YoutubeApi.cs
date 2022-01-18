using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SimpleLogger;

namespace YoutubeApi
{
    public class YoutubeApi
    {
        private readonly YouTubeService youtubeService;
        private readonly Logger logger = new("YoutubeApi");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="apiKey"></param>
        /// <param name="theLogger"></param>
        public YoutubeApi(string applicationName, string apiKey, Logger? theLogger = null)
        {
            if (theLogger != null)
            {
                this.logger = theLogger;
            }

            try
            {
                this.youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = apiKey,
                    ApplicationName = applicationName
                });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Returns the newest and a maximum of 'maxResults' videos of the channel channelId.
        /// </summary>
        /// <param name="channelId">Id of channel</param>
        /// <param name="maxResults">Maximum amount of videos</param>
        /// <returns>List of videos</returns>
        public async Task<List<Video>> GetVideosOfChannel(string channelId, int maxResults)
        {
            var listOfChannelVideos = new List<Video>();
            try
            {
                var searchListRequest = this.youtubeService.Search.List("snippet");
                searchListRequest.ChannelId = channelId;
                searchListRequest.Type = "video";
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.MaxResults = maxResults;

                // Call the search.list method to retrieve results matching the specified query term.
                var searchListResponse = await searchListRequest.ExecuteAsync();

                // Add each result to the appropriate list, and then display the lists of
                // matching videos, channels, and playlists.
                foreach (var searchResult in searchListResponse.Items)
                {
                    if (searchResult.Id.Kind == "youtube#video")
                    {
                        var videoRequest = youtubeService.Videos.List("snippet");
                        videoRequest.Id = searchResult.Id.VideoId;
                        var result = await videoRequest.ExecuteAsync();

                        foreach (var channelVideo in result.Items)
                        {
                            var newVideo = new Video
                            {
                                Title = channelVideo.Snippet.Title,
                                Id = channelVideo.Id,
                                ChannelId = channelVideo.Snippet.ChannelId,
                                ChannelTitle = channelVideo.Snippet.ChannelTitle,
                                Description = channelVideo.Snippet.Description
                            };

                            if (channelVideo.Snippet.PublishedAt != null)
                            {
                                newVideo.PublishedAtRaw = channelVideo.Snippet.PublishedAt.Value;
                            }

                            listOfChannelVideos.Add(newVideo);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }

            return listOfChannelVideos;
        }

        public void CreateVideoFile(List<string> channelIds, int maximumResult)
        {

            var tasks = channelIds.Select(channelId => GetVideosOfChannel(channelId, maximumResult)).ToArray();
            Task.WaitAll(tasks);

            var listOfVideoLists = tasks.Select(task => task.Result);
            List<Video> completeVideoList = new List<Video>(maximumResult * channelIds.Count);
            foreach (var videos in listOfVideoLists)
            {
                completeVideoList.AddRange(videos);
            }

            var myVideos = new YtVideos
            {
                Videos = completeVideoList
            };

            YtVideos.SerializeObject(myVideos);
        }
    }
}