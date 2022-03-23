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
        private readonly string listOfProcessedFilesWorker01, list_Gayman, list_Schwuchteln, list_DSBM;
        private bool workersRun;
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
            this.list_Gayman = Path.Combine(this.WorkDir, $"__{nameof(this.list_Gayman)}.list");
            this.list_Schwuchteln = Path.Combine(this.WorkDir, $"__{nameof(this.list_Schwuchteln)}.list");
            this.list_DSBM = Path.Combine(this.WorkDir, $"__{nameof(this.list_DSBM)}.list");
            this.facebookConfig = fbConfig;
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopAllWorker()
        {
            this.workersRun = false;
            this.logger.LogWarning("All Facebook workers are in standby now.");
        }

        public async Task StartFbWorker01()
        {
            this.workersRun = true;
            await Task.Run(() =>
                           {
                               while (this.workersRun)
                               {
                                   PrepareAndSendToGroups(this.listOfProcessedFilesWorker01,
                                                          this.facebookConfig.TaskGroups_01);
                                   Thread.Sleep(GetSleepTime());
                               }
                           });
        }

        public async Task StartGaymanWorker()
        {
            this.workersRun = true;
            await Task.Run(() =>
                           {
                               while (this.workersRun)
                               {
                                   PrepareAndSendToGroups(this.list_Gayman,
                                                          this.facebookConfig.TestGroups,
                                                          true);
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
        /// TODO Kommentar anpassen
        /// This task has 4 subtasks:
        /// 1 Find not yet processed youtube meta files and a list of video meta data objects out of it
        /// 2 Call SendVideoToGroups for each video meta data object.
        ///    --> This methods does "Update file with processed files" internally
        /// 3 Trim the file within the processed file names
        /// </summary>
        /// <param name="pathToProcessedFiles">Path to the file that contains the names and paths of the processed videos.</param>
        /// <param name="faceBookGroups">The FB groups to send in.</param>
        /// <param name="specialSensitive">Diese Deutschen sind zum Teil sehr spezielle Exemplare der menschlichen Gattung. Das zeigt sich an vielen
        /// Stellen im echten und virtuellen Leben. FB-Gruppen, die nur Posts mit deutschen Projekten zulassen, sind dabei beinahe noch erträglich.
        /// Aber lassen wir das. Ist dieser Schalter aktiv, werden ausschließlich "deutsche" Videos in Gruppen veröffentlicht.</param>
        /// <returns></returns>
        public void PrepareAndSendToGroups(string pathToProcessedFiles,
                                           List<Group> faceBookGroups,
                                           bool specialSensitive = false)
        {
            try
            {
                // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                // It is probably not necessary to secure this process with a mutex, because each bot must manage
                // its own list of already processed files.
                var notYetProcessed =
                    FileHandling.FindNotYetProcessedVideoIdFiles(pathToProcessedFiles,
                                                                 this.WorkDir,
                                                                 VideoMetaDataFull.VideoFileSearchPattern);

                var videos = VideoMetaDataFull.DeserializeFiles(notYetProcessed, out var filesNotFound);
                filesNotFound.ForEach(file => this.logger.LogWarning($"{file} not found"));

                if (specialSensitive)
                {
                    DoSpecialGermanShit(pathToProcessedFiles, videos, faceBookGroups);
                    return;
                }

                videos.ForEach(video =>
                               {
                                   SendVideoToGroups(video, faceBookGroups, pathToProcessedFiles);
                               });
                FileHandling.TrimFileListOfProcessedFile(pathToProcessedFiles);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// 1 Call SendVideoToGroups for each video meta data object.
        ///    --> This methods does "Update file with processed files" internally
        ///        or its no german video, so "Update file with processed files" is done in here.
        /// 2 Trim the file within the processed file names
        /// </summary>
        public void DoSpecialGermanShit(string pathToProcessedFiles, List<VideoMetaDataFull> videos, List<Group> faceBookGroups)
        {
            videos.ForEach(video =>
                           {
                               if (video.IsGerman())
                               {
                                   SendVideoToGroups(video, faceBookGroups, pathToProcessedFiles);
                               }
                               else
                               {
                                   // If not german add it to the list for avoiding processing a second time
                                   FileHandling.AppendFilePathsToProcessedFilesList(pathToProcessedFiles,
                                                                                    new List<string> { video.GetFullPathToVideo(this.WorkDir) });
                               }
                           });
            FileHandling.TrimFileListOfProcessedFile(pathToProcessedFiles);
        }


        /// <summary>
        /// Here and in the internally called method, the video is ultimately posted to the Facebook groups passed.
        /// A loop goes sequentially and in the same thread through all groups and publishes the post.
        /// </summary>
        /// <param name="video">This will be published.</param>
        /// <param name="fbGroups">In these groups it will be published.</param>
        /// <param name="pathToProcessedFiles">Path to the file that contains the names and paths of the processed videos.</param>
        /// <returns></returns>
        public void SendVideoToGroups(VideoMetaDataFull video, List<Group> fbGroups, string pathToProcessedFiles)
        {
            var result = 0;
            var fbAuto = new FacebookAutomation(this.WorkDir, this.logger);
            fbAuto.Login(this.facebookConfig.Email, this.facebookConfig.Pw);

            fbGroups.ForEach(group =>
                             {
                                 var msg = " posted video: " + video.Title;
                                 msg += " in FB group: " + group.GroupName;
                                 var fbDescription = video.GetFacebookDescription();
                                 if (InternalPublish(fbAuto, group, fbDescription, msg)) result++;
                             });
            fbAuto.Dispose();

            if (result == 0)
            {
                this.logger.LogError($"{video.Title} couldn't be sent. Worker file: {pathToProcessedFiles} ");
            }
            else
            {
                this.logger.LogInfo($"{video.Title} successfully sent. Worker file: {pathToProcessedFiles} ");

                // This may seem a little awkward. Each video is thus added individually to this list of processed files.
                FileHandling.AppendFilePathsToProcessedFilesList(pathToProcessedFiles,
                                                                 new List<string> { video.GetFullPathToVideo(this.WorkDir) });
            }
        }

        /// <summary>
        /// The method mainly regulates sufficiently detailed logging. The method mainly regulates sufficiently detailed logging. 
        /// Before this method is called, logging in must be completed successfully.
        /// </summary>
        /// <param name="fbAuto">Handle on logged in FB automation</param>
        /// <param name="theGroup">Group to send the publicationMessage</param>
        /// <param name="publicationMessage">Message to publish in Facebook group</param>
        /// <param name="logMessage">Message to log.</param>
        /// <returns></returns>
        private bool InternalPublish(FacebookAutomation fbAuto, Group theGroup, string publicationMessage, string logMessage)
        {
            bool result = false;
            string msg, logInfo;
            if (fbAuto.PublishToGroup(theGroup, publicationMessage))
            {
                result = true;
                msg = "Successful " + logMessage;
                logInfo = "INFO  ***";
                this.logger.LogInfo(msg);
            }
            else
            {
                result = false;
                msg = "Error. Not " + logMessage;
                logInfo = "ERROR ***";
                this.logger.LogError(msg);
            }

            if (this.sendDebugMessage != null)
            {
                this.sendDebugMessage(logInfo, msg);
            }

            return result;
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
