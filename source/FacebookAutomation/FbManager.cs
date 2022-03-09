using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using SimpleLogger;

namespace FacebookAutomation
{
    public class FbManager
    {
        private readonly Logger logger;
        private readonly string listOfProcessedFilesWorker01;
        private bool fbWorkerShallRun_01;
        private readonly Action<string, string> sendDebugMessage;
        private readonly FacebookConfig facebookConfig;

        /// <summary>
        /// The WorkDir contains all the files needed for the TelegramManager:
        /// 1.  Subfolders in working directory within lists of files within VideoMetaDataFull per video to check which videos
        ///     have been published on the channels.
        /// 2.  One 'ListOfProcessedFiles' file per task and bot to check which videos from the VideoMetaDataFull files have
        /// already been processed by a bot in a specific task.
        /// </summary>
        public readonly string WorkDir;

        public FbManager(string workDir, FacebookConfig fbConfig, Action<string, string> callback = null)
        {
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }
            this.WorkDir = workDir;
            this.logger = new Logger("FbManager.log");

            if (callback != null)
            {
                this.sendDebugMessage = callback;
            }

            this.listOfProcessedFilesWorker01 = Path.Combine(this.WorkDir, $"__FaceBookWorker_01.list");
            this.facebookConfig= fbConfig;
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopAllWorker()
        {
            this.fbWorkerShallRun_01 = false;
            this.logger.LogWarning("All Facebook workers are in standby now.");
        }

        public async Task StartFbWorker01()
        {
            this.fbWorkerShallRun_01 = true;
            await Task.Run(() =>
                           {
                               while (this.fbWorkerShallRun_01)
                               {
                                   if (!SendVideoDataIntoGroupsAsync(this.listOfProcessedFilesWorker01,
                                                                     400,
                                                                     this.facebookConfig.TestGroups,
                                                                     false).Wait(TimeSpan.FromSeconds(60)))
                                   {
                                       this.logger.LogWarning("TimeOut in FbWorker01 async. Check it.");
                                       this.sendDebugMessage?.Invoke("WARN ***", "TimeOut in FbWorker01 async. Check it.");
                                   }

                                   Thread.Sleep(GetSleepTime());
                               }
                           });
        }

        /// <summary>
        /// This method enforces a sleep time for worker threads depending on the current time.
        /// </summary>
        /// <returns>Dynamic sleep time depending on the time of day.</returns>
        public TimeSpan GetSleepTime()
        {
            return TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Method executes a task and send messages into theChat by using theBot.
        /// This task has 4 subtasks:
        /// 1 Find not yet processed youtube meta files
        /// 2 Send not yet processed files in the chat theChat
        /// 3 Update file with processed files
        /// 4 Trim the file within the processed file names
        /// </summary>
        /// <param name="pathToProcessedFiles">Path to the file that contains the names and pathes of the processed videos.</param>
        /// <param name="sizeOfFile">Size of file in lines</param>
        /// <param name="faceBookGroups">The FB groups to send in.</param>
        /// <returns></returns>
        public async Task SendVideoDataIntoGroupsAsync(string pathToProcessedFiles,
                                                       int sizeOfFile,
                                                       List<Group> faceBookGroups,
                                                       bool gaymanSensitive = false)
        {
            try
            {
                var offsetSecs = 30;
                var secsPerGroup = 30;

                await Task.Run(() =>
                {
                    // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                    // It is probably not necessary to secure this process with a mutex, because each bot must manage
                    // its own list of already processed files.
                    var notYetProcessed =
                        FileHandling.FindNotYetProcessedVideoIdFiles(pathToProcessedFiles,
                                                                     this.WorkDir,
                                                                     VideoMetaDataFull.VideoFileSearchPattern);

                    if (notYetProcessed.Count > 0)
                    {
                        if (SendVideoMetaDataToGroupsAsync(notYetProcessed, faceBookGroups, gaymanSensitive)
                            .Wait(TimeSpan.FromSeconds(offsetSecs + faceBookGroups.Count * secsPerGroup * notYetProcessed.Count)))
                        {
                            FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(pathToProcessedFiles, notYetProcessed);
                            FileHandling.TrimFileListOfProcessedFile(pathToProcessedFiles, sizeOfFile);
                        }
                        else
                        {
                            this.logger.LogError("Timeout in SomeBotToHaufenChatTaskAsync. Don't just stand there, kill something!");
                        }
                    }
                });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notYetProcessed"></param>
        /// <param name="faceBookGroups"></param>
        /// <param name="gaymanSensitive"></param>
        /// <returns></returns>
        public async Task SendVideoMetaDataToGroupsAsync(List<string> notYetProcessed,
                                                         List<Group> faceBookGroups,
                                                         bool gaymanSensitive = false)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   var listOfMetaVideoDate = VideoMetaDataFull.DeserializeFiles(notYetProcessed, out var filesNotFound);
                                   filesNotFound.ForEach(file => this.logger.LogWarning($"{file} not found"));

                                   if (listOfMetaVideoDate.Count > 0)
                                   {
                                       var fbAuto = new FacebookAutomation(this.WorkDir, this.logger);
                                       fbAuto.Login(this.facebookConfig.Email, this.facebookConfig.Pw);

                                       listOfMetaVideoDate.ForEach(video =>
                                                                   {
                                                                       var fbDescription = video.GetFacebookDescription();

                                                                       if (gaymanSensitive)
                                                                       {
                                                                           if (video.IsGerman())
                                                                           {
                                                                               faceBookGroups.ForEach(group =>
                                                                               {
                                                                                   fbAuto.PublishTextContentInFaceBookGroup(group.GroupId, fbDescription);
                                                                               });
                                                                           }
                                                                       }
                                                                       else
                                                                       {
                                                                           faceBookGroups.ForEach(group =>
                                                                                                  {
                                                                                                      fbAuto.PublishTextContentInFaceBookGroup(group.GroupId, fbDescription);
                                                                                                  });
                                                                       }
                                                                   });

                                       fbAuto.Dispose();
                                       this.logger.LogInfo($"Send Infos for videos to facebook groups");
                                   }
                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// Publish video on own site.
        /// </summary>
        /// <param name="video">The video</param>
        /// <returns></returns>
        public async Task SendVideoToOwnSite(VideoMetaDataFull video)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   //var fbAuto = new FacebookAutomation(this.WorkDir, this.logger);
                                   //fbAuto.Login(this.facebookConfig.Email, this.facebookConfig.Pw);

                                   var fbDescription = video.GetFacebookDescription();
                                   //fbAuto.PublishTextOnOwnFbSite(fbDescription);
                                   this.logger.LogInfo($"Send Infos for video {video.Id} to oen facebook site");

                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }
    }
}
