﻿using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using SimpleLogger;

namespace YoutubeApi
{
    public class YoutubeApi
    {
        private readonly YouTubeService youtubeService;
        private readonly Logger logger = new("YoutubeApi");

        /// <summary>
        /// Ctor.
        /// Initiates the YoutubeService.
        /// </summary>
        /// <param name="applicationName">Name of application (irrelevant)</param>
        /// <param name="apiKey">Secret api key</param>
        /// <param name="theLogger">Logger if available</param>
        public YoutubeApi(string applicationName, string apiKey, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger("YoutubeApi.log");

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
        /// <param name="channelId">Id of channel</param>
        /// <param name="maxResults">Maximum amount of videos</param>
        /// <returns>List of videos</returns>
        private async Task<List<VideoMetaDataFull>> GetFullVideoMetaDatasOfChannel(string channelId, int maxResults)
        {
            var listOfChannelVideos = new List<VideoMetaDataFull>();
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
                            var newVideo = new VideoMetaDataFull
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelIds"></param>
        /// <param name="maximumResult"></param>
        public void CreateVideoFile(List<string> channelIds, int maximumResult)
        {
            var tasks = channelIds.Select(channelId => GetFullVideoMetaDatasOfChannel(channelId, maximumResult)).ToArray();
            Task.WaitAll(tasks);

            var listOfVideoLists = tasks.Select(task => task.Result);
            List<VideoMetaDataFull> completeVideoList = new List<VideoMetaDataFull>(maximumResult * channelIds.Count);
            foreach (var videos in listOfVideoLists)
            {
                completeVideoList.AddRange(videos);
            }

            VideoMetaDataFull.Serialize(completeVideoList);
        }
    }
}