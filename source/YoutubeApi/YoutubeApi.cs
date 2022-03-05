using System.Text.Json;
using Common;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using SimpleLogger;

namespace YoutubeApi
{
    public class YoutubeApi
    {
        private readonly string workDir;
        private readonly string mainApiKey;
        public Logger Logger { get; }

        /// <summary>
        /// Ctor.
        /// Initiates the YoutubeService.
        /// </summary>
        /// <param name="apiKey">Secret api key</param>
        /// <param name="workDir">Working directory for video meta data files</param>
        /// <param name="theLogger">Logger if available</param>
        public YoutubeApi(string apiKey, string workDir, Logger? theLogger = null)
        {
            Logger = theLogger ?? new Logger("YoutubeApi.log");
            try
            {
                this.workDir = workDir;
                if (!Directory.Exists(workDir))
                {
                    Directory.CreateDirectory(workDir);
                }

                this.mainApiKey = apiKey;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates youtube service.
        /// </summary>
        /// <returns>The youtube service</returns>
        public YouTubeService GetYoutubeService()
        {
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = this.mainApiKey,
                ApplicationName = "MyYoutubeApiHasNoWinkle"
            });
            return service;
        }

        /// <summary>
        /// This method returns a list within all meta data to videos found in the passed channel 'channel'.
        /// With the help of the id of a found video, it is checked whether the working subfolder already contains files whose names contain the
        /// id of this found video.
        /// Because this method accesses the subfolder in the working directory, this subfolder is created if it does not yet exist.
        /// </summary>
        /// <param name="channel">The Youtube channel.</param>
        /// <param name="maximumResult">Consider only this amount of results.</param>
        /// <returns>The list of full meta data videos found in the channel.</returns>
        public async Task<List<VideoMetaDataFull>> GetFullVideoMetaDataOfChannelAsync(Channel channel,
                                                                                      int maximumResult)
        {
            Logger.LogDebug($"Check {channel.ChannelName} with id {channel.ChannelId}");

            if (!Directory.Exists(Path.Combine(this.workDir, channel.ChannelId)))
            {
                Directory.CreateDirectory(Path.Combine(this.workDir, channel.ChannelId));
            }

            // This list contains all videos of the 'channel' including the complete 'Description'.
            var resultListOfChannelVideos = new List<VideoMetaDataFull>(maximumResult);
            try
            {
                var service = GetYoutubeService();
                var playlistItemsListRequest = service.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = channel.ChannelUploadsPlayListId;
                playlistItemsListRequest.MaxResults = maximumResult;

                // The result of this request contains at least the VideoIds of the videos published on this channel (maybe other stuff too?). 
                // The complete description of the videos is created in a separate method.
                var playlistItemsResponse = await playlistItemsListRequest.ExecuteAsync();
                var playlistItems = playlistItemsResponse.Items.ToList();
                service.Dispose();

                // Check the working directory to see if it already contains one or more of the videos found in 'playlistItems'.
                // This method only returns the videosIds that are not yet in the working directory.
                var listOfVideoIds = GetListOfVideoIdsNotYetInWorkSubDir(channel, playlistItems);
                Logger.LogDebug($"In {channel.ChannelName} found {listOfVideoIds.Count} new videos.");

                if (listOfVideoIds.Count > 0)
                {
                    // For each videoId, perform a separate videoRequest. This means that for each video found,
                    // another request is made to obtain the complete "Description". This is parallelized.
                    var tasks = listOfVideoIds.Select(GetVideoMetaData).ToArray();
                    Task.WaitAll(tasks);
                    var listOfVideoLists = tasks.Select(task => task.Result);
                    resultListOfChannelVideos.AddRange(listOfVideoLists);

                    // Log videos found in one channel
                    Logger.LogDebug(
                        $"Found videos in {channel.ChannelName}: {CreateMessageWithVideoDataMetaInformation(resultListOfChannelVideos)}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Unknown error, Exception is catched work should go on." + Environment.NewLine + e.Message);
            }

            return resultListOfChannelVideos;
        }

        /// <summary>
        /// This method only returns the videosIds that are not yet in the working subdirectory.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="playlistItems">Items found in the playlist.</param>
        /// <returns>List of videos published since last successful check.</returns>
        private List<string> GetListOfVideoIdsNotYetInWorkSubDir(Channel channel, List<PlaylistItem> playlistItems)
        {
            var listOfVideoIds = playlistItems.Select(item => item.Snippet.ResourceId.VideoId).ToList();
            return FileHandling.ReduceListOfIds(listOfVideoIds, 
                                                Path.Combine(this.workDir, channel.ChannelId), 
                                                VideoMetaDataFull.VideoFileSearchPattern);
        }

        /// <summary>
        /// This method performs a VideoRequest for a single video to get the full description of the video.
        /// The confusing thing about this construct is that the result of this call is in a list.This is due to the YoutubeApi.
        /// </summary>
        /// <param name="videoId">The video to which the information is fetched.</param>
        /// <returns>This list contains exactly one video. At the moment I'm not sure how awesome it is:-(</returns>
        public async Task<VideoMetaDataFull> GetVideoMetaData(string videoId)
        {
            var newVideo = new VideoMetaDataFull();

            try
            {
                var service = GetYoutubeService();
                var videoRequest = service.Videos.List("snippet");
                videoRequest.Id = videoId;
                var videoLisResponse = await videoRequest.ExecuteAsync();

                // There should be only one element in this collection
                if (videoLisResponse.Items.Count != 1)
                {
                    Logger.LogWarning($"Video.List response of video {videoId} returned collection with {videoLisResponse.Items.Count} elements");
                }

                // Yes, full intention. It's supposed to bang here when there's nothing in it.
                var video = videoLisResponse.Items.First();

                newVideo = new VideoMetaDataFull
                {
                    Title = video.Snippet.Title,
                    TitleBase64 = VideoMetaDataFull.Base64Encode(video.Snippet.Title),
                    Id = video.Id,
                    ChannelId = video.Snippet.ChannelId,
                    ChannelTitle = video.Snippet.ChannelTitle,
                    DescriptionBase64 = VideoMetaDataFull.Base64Encode(video.Snippet.Description)
                };

                try
                {
                    newVideo.PublishedAtRaw = DateTimeOffset.Parse(video.Snippet.PublishedAtRaw).UtcDateTime;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                    newVideo.PublishedAtRaw = DateTime.UtcNow;
                }

                service.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError("Error, but go on." + Environment.NewLine + e.Message);
            }

            return newVideo;
        }


        /// <summary>
        /// Async Method to create a list with all the metadata of the videos contained in the channels in channelIds.
        /// </summary>
        /// <remarks>
        /// A separate file is created for each channel video found. Such a file always contains the id of the video. This makes it possible to
        /// check which of these videos have already been saved as a file and which have not.
        /// </remarks>
        /// <param name="channelIds">List of Youtube channels.</param>
        /// <param name="maxResultPerChannel">Consider only this amount of results per channel.</param>
        /// <returns>List with videos if news available, empty list if not.</returns>
        public async Task<List<VideoMetaDataFull>> CreateListWithFullVideoMetaDataAsync(List<Channel> channelIds,
                                                                                        int maxResultPerChannel)
        {
            List<VideoMetaDataFull> completeVideoList = new(maxResultPerChannel * channelIds.Count);
            await Task.Run(() =>
                           {
                               var tasks = channelIds
                                           .Select(channelId => GetFullVideoMetaDataOfChannelAsync(
                                                       channelId,
                                                       maxResultPerChannel)).ToArray();
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
        /// Creates a readable string containing one video title per line.
        /// </summary>
        /// <param name="listOfVideoMetaFiles">List of videos</param>
        /// <returns>readable string</returns>
        public static string CreateMessageWithVideoDataMetaInformation(List<VideoMetaDataFull> listOfVideoMetaFiles)
        {
            var message = "Created files successfully within this videos:" + Environment.NewLine;
            foreach (var video in listOfVideoMetaFiles)
            {
                message += VideoMetaDataFull.Base64Decode(video.TitleBase64) + $" - {video.Id}" + Environment.NewLine;
            }

            return message;
        }
    }
}