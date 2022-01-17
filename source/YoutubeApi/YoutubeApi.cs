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
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        public async void ReadChannelList(string channelId, int maxResults)
        {
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
                        var result = videoRequest.Execute();

                        foreach (var video in result.Items)
                        {

                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }
    }
}