using Common;
using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        private readonly Logger logger;
        private bool someBotToHaufenChatWorkerShallRun, blackmetaloidRun, blackMetaloidRunGerman;

        private readonly TelegramBot irgendeinBot, unh317_01Bot, blackmetaloidBot;
        private readonly Chat haufenChat, gBmChat, bMChat, debugChannel;

        private readonly string irgendeinBotListOfProcessedFiles;
        private readonly string blackmetaloidBotListOfProcessedFiles, blackmetaloidBotListOfProcessedFilesGerman;
        private readonly string unh317_01BotListOfProcessedFiles;

        /// <summary>
        /// The WorkDir contains all the files needed for the TelegramManager:
        /// 1.  Subfolders in working directory within lists of files within VideoMetaDataFull per video to check which videos
        ///     have been published on the channels.
        /// 2.  One 'ListOfProcessedFiles' file per task and bot to check which videos from the VideoMetaDataFull files have
        /// already been processed by a bot in a specific task.
        /// </summary>
        public readonly string WorkDir;

        /// <summary>
        /// Ctor.
        /// </summary>
        public TelegramManager(TelegramConfig myConfig, string youtubeSearchPattern, string workDir)
        {
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }
            this.WorkDir = workDir;

            var unh31701Config = myConfig.Bots[0];
            var irgendeinBotConfig = myConfig.Bots[1];
            var blackmetaloidConfig = myConfig.Bots[2];

            this.haufenChat = myConfig.Chats[0];
            this.gBmChat = myConfig.Chats[1];
            this.bMChat = myConfig.Chats[2];
            this.debugChannel = myConfig.Chats[3];

            this.logger = new Logger("TelegramManager.log");

            this.irgendeinBot = new TelegramBot(irgendeinBotConfig.BotToken, irgendeinBotConfig.BotName);
            this.blackmetaloidBot = new TelegramBot(blackmetaloidConfig.BotToken, blackmetaloidConfig.BotName);
            this.unh317_01Bot = new TelegramBot(unh31701Config.BotToken, unh31701Config.BotName);


            this.irgendeinBotListOfProcessedFiles = Path.Combine(this.WorkDir, $"__{this.irgendeinBot.Name}.list");
            this.blackmetaloidBotListOfProcessedFiles = Path.Combine(this.WorkDir, $"__{this.blackmetaloidBot.Name}.list");
            this.blackmetaloidBotListOfProcessedFilesGerman =
                Path.Combine(this.WorkDir, $"__{this.blackmetaloidBot.Name}_German.list");
            this.unh317_01BotListOfProcessedFiles = Path.Combine(this.WorkDir, $"__{this.unh317_01Bot.Name}.list");

            this.logger.LogDebug("Ctor successful");
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopAllWorker()
        {
            this.someBotToHaufenChatWorkerShallRun = false;
            this.blackMetaloidRunGerman = false;
            this.blackmetaloidRun = false;
            this.logger.LogWarning("All telegram bots are in standby now.");
        }

        /// <summary>
        /// Telegram worker is an internal while one loop that runs inside a task and with a pause of 30 seconds after doing work
        /// or
        /// initiate doing work..
        /// </summary>
        /// <returns>Returns the task to have the possibility to wait for it, even if no one would do that.</returns>
        public async Task StartSomeBotToHaufenChat()
        {
            this.someBotToHaufenChatWorkerShallRun = true;
            await Task.Run(() =>
                           {
                               while (this.someBotToHaufenChatWorkerShallRun)
                               {
                                   if (!SendVideoDataIntoChatAsync(this.irgendeinBotListOfProcessedFiles,
                                                                   400,
                                                                   this.irgendeinBot,
                                                                   this.haufenChat).Wait(TimeSpan.FromSeconds(45)))
                                   {
                                       this.logger.LogWarning("TimeOut in IrgendeinBotTask async. Check it.");
                                       _ = SendDebugMessageAsync("TimeOut in IrgendeinBotTask async. Check it.");
                                   }

                                   Thread.Sleep(GetSleepTime());
                               }
                           });
        }

        public async Task StartBlackMetaloidToBmChat()
        {
            this.blackmetaloidRun = true;
            await Task.Run(() =>
                           {
                               while (this.blackmetaloidRun)
                               {
                                   if (!SendVideoDataIntoChatAsync(this.blackmetaloidBotListOfProcessedFiles,
                                                                   400,
                                                                   this.blackmetaloidBot,
                                                                   this.bMChat).Wait(TimeSpan.FromSeconds(45)))
                                   {
                                       this.logger.LogWarning("TimeOut in BlackMetaloid async. Check it.");
                                       _ = SendDebugMessageAsync("TimeOut in BlackMetaloid async. Check it.");
                                   }

                                   Thread.Sleep(GetSleepTime());
                               }
                           });
        }

        public async Task StartGermanBlackMetaloidToGbmChat()
        {
            this.blackMetaloidRunGerman = true;
            await Task.Run(() =>
                           {
                               while (this.blackMetaloidRunGerman)
                               {
                                   if (!SendVideoDataIntoChatAsync(this.blackmetaloidBotListOfProcessedFilesGerman,
                                                                   400,
                                                                   this.blackmetaloidBot,
                                                                   this.gBmChat,
                                                                   true).Wait(TimeSpan.FromSeconds(45)))
                                   {
                                       this.logger.LogWarning("TimeOut in GermanBlackMetaloid async. Check it.");
                                       _ = SendDebugMessageAsync("TimeOut in GermanBlackMetaloid async. Check it.");
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
        /// <param name="theBot">The bot that is used to send</param>
        /// <param name="theChat">The chat to send in.</param>
        /// <param name="gaymanSensitive">If true, Description is checked for gayman black metal</param>
        /// <returns></returns>
        public async Task SendVideoDataIntoChatAsync(string pathToProcessedFiles,
                                                     int sizeOfFile,
                                                     TelegramBot theBot,
                                                     Chat theChat,
                                                     bool gaymanSensitive = false)
        {
            try
            {
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
                                       if (SendVideoMetaDataToChatAsync(notYetProcessed, theBot, theChat, gaymanSensitive)
                                           .Wait(TimeSpan.FromSeconds(5 + notYetProcessed.Count * 2)))
                                       {
                                           FileHandling.AppendFilePathsToProcessedFilesList(pathToProcessedFiles, notYetProcessed);
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
        /// Method publishes all entries of 'VideoMetaDataFull' that could be found in the the files in notYetProcessedFiles in
        /// theChat via theBot.
        /// </summary>
        /// <param name="notYetProcessedFiles"></param>
        /// <param name="theBot"></param>
        /// <param name="theChat"></param>
        /// <param name="gaymanSensitive"></param>
        /// <returns></returns>
        private async Task SendVideoMetaDataToChatAsync(List<string> notYetProcessedFiles,
                                                        TelegramBot theBot,
                                                        Chat theChat,
                                                        bool gaymanSensitive = false)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   var listOfMetaVideoDate = VideoMetaDataFull.DeserializeFiles(notYetProcessedFiles, out var filesNotFound);
                                   filesNotFound.ForEach(file => this.logger.LogWarning($"{file} not found"));

                                   if (listOfMetaVideoDate.Count > 0)
                                   {
                                       // Minimum timeout plus the amount of videos found in file newNotProcessedFile
                                       var timeOut = 10 + listOfMetaVideoDate.Count * 2;
                                       this.logger.LogDebug(
                                           $"Bot {theBot.Name} start sending to {theChat.ChatName} with id {theChat.ChatId}. " +
                                           $"Timeout {timeOut} seconds. Videos {listOfMetaVideoDate.Count}");

                                       var theTasks = new List<Task>();
                                       listOfMetaVideoDate.ForEach(video =>
                                                                   {
                                                                       if (!gaymanSensitive)
                                                                       {
                                                                           theTasks.Add(theBot.SendToChatAsync(
                                                                               theChat,
                                                                               video.GetReadableDescription(),
                                                                               timeOut));
                                                                       }
                                                                       else if (video.IsGerman())
                                                                       {
                                                                           theTasks.Add(theBot.SendToChatAsync(
                                                                                            theChat,
                                                                                            video.GetReadableDescription(),
                                                                                            timeOut));
                                                                       }
                                                                   });

                                       if (theTasks.Count > 0)
                                       {
                                           Task.WaitAll(theTasks.ToArray());
                                           this.logger.LogInfo($"Bot {theBot.Name} sent to chat {theChat.ChatName} {listOfMetaVideoDate.Count} videos.");
                                       }
                                   }
                                   else
                                   {
                                       this.logger.LogInfo($"Nothing to do for {theBot.Name} in {theChat.ChatName}");
                                   }
                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// Send a message tot the debug channel.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendDebugMessageAsync(string message)
        {
            try
            {
                await this.irgendeinBot.SendToChatAsync(this.debugChannel, message, TimeSpan.FromSeconds(15).Seconds);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }
    }
}