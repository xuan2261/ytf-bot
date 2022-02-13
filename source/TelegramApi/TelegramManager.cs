using Common;
using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        private readonly Logger logger;
        private bool workerShallRun;

        // ReSharper disable once InconsistentNaming
        private readonly TelegramBot irgendeinBot, unh317_01Bot, blackmetaloidBot;
        private readonly Chat haufenChat, gBmChat, bMChat, debugChannel;

        private readonly string irgendeinBotListOfProcessedFiles;
        private readonly string blackmetaloidBotListOfProcessedFiles;

        // ReSharper disable once InconsistentNaming
        private readonly string unh317_01BotListOfProcessedFiles;
        private readonly string youtubeSearchPattern;

        /// <summary>
        /// The WorkDir contains all the files needed for the TelegramManager:
        /// 1.  VideoMetaDataFull to check which videos have been published on the channels.
        /// 2.  One 'ListOfProcessedFiles' file per task and bot to check which videos from the VideoMetaDataFull files have
        /// already been
        /// processed by a bot in a specific task.
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

            this.youtubeSearchPattern = youtubeSearchPattern;

            this.irgendeinBotListOfProcessedFiles = Path.Combine(this.WorkDir, $"{this.irgendeinBot.Name}.list");
            this.blackmetaloidBotListOfProcessedFiles = Path.Combine(this.WorkDir, $"{this.blackmetaloidBot.Name}.list");
            this.unh317_01BotListOfProcessedFiles = Path.Combine(this.WorkDir, $"{this.unh317_01Bot.Name}.list");

            this.logger.LogDebug("Ctor successful");
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopYoutubeWorker()
        {
            this.workerShallRun = false;
            this.logger.LogWarning("All telegram bots are in standby now.");
        }

        /// <summary>
        /// Telegram worker is an internal while one loop that runs inside a task and with a pause of 30 seconds after doing work
        /// or
        /// initiate doing work..
        /// </summary>
        /// <returns>Returns the task to have the possibility to wait for it, even if no one would do that.</returns>
        public async Task StartTelegramWorker()
        {
            this.workerShallRun = true;
            await Task.Run(() =>
                           {
                               while (this.workerShallRun)
                               {
                                   if (!IrgendeinBotTaskAsync().Wait(TimeSpan.FromSeconds(10)))
                                   {
                                       this.logger.LogWarning("TimeOut in IrgendeinBotTask async. Check it.");
                                       _ = SendDebubMessageAsync("TimeOut in IrgendeinBotTask async. Check it.");
                                   }

                                   Thread.Sleep(TimeSpan.FromMinutes(1));
                               }
                           });
        }

        /// <summary>
        /// Method executes a task by using the bot irgendeinBot.
        /// This task has 4 subtasks:
        /// 1 Find not yet processed youtube meta files
        /// 2 Send not yet processed files in the chat haufenChat
        /// 3 Update file with processed files
        /// 4 Trim the file within the processed file names
        /// </summary>
        /// <returns>Task</returns>
        public async Task BlackmetaloidBotTaskAsync()
        {
            try
            {
                await Task.Run(() =>
                               {
                                   // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                                   // It is probably not necessary to secure this process with a mutex, because each bot must manage
                                   // its own list of already processed files.
                                   var notYetProcessed = FileHandling.FindNotYetProcessedYoutubeMetaFiles(this.blackmetaloidBotListOfProcessedFiles,
                                       this.WorkDir,
                                       this.youtubeSearchPattern);

                                   if (SendYoutubeMetaFileInfoToTelegramChatAsync(notYetProcessed, this.blackmetaloidBot, this.bMChat)
                                       .Wait(TimeSpan.FromSeconds(10)))
                                   {
                                       FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(this.blackmetaloidBotListOfProcessedFiles,
                                           notYetProcessed);
                                       FileHandling.TrimFileListOfProcessedFile(this.blackmetaloidBotListOfProcessedFiles, 50);
                                   }
                                   else
                                   {
                                       this.logger.LogError("Timeout in IrgendeinBotTaskAsync. Don't just stand there, kill something!");
                                   }
                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// Method executes a task by using the bot irgendeinBot.
        /// This task has 4 subtasks:
        /// 1 Find not yet processed youtube meta files
        /// 2 Send not yet processed files in the chat haufenChat
        /// 3 Update file with processed files
        /// 4 Trim the file within the processed file names
        /// </summary>
        /// <returns>Task</returns>
        public async Task IrgendeinBotTaskAsync()
        {
            try
            {
                await Task.Run(() =>
                               {
                                   // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                                   // It is probably not necessary to secure this process with a mutex, because each bot must manage
                                   // its own list of already processed files.
                                   var notYetProcessed = FileHandling.FindNotYetProcessedYoutubeMetaFiles(this.irgendeinBotListOfProcessedFiles,
                                       this.WorkDir,
                                       this.youtubeSearchPattern);

                                   if (SendYoutubeMetaFileInfoToTelegramChatAsync(notYetProcessed, this.irgendeinBot, this.haufenChat)
                                       .Wait(TimeSpan.FromSeconds(10)))
                                   {
                                       FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(this.irgendeinBotListOfProcessedFiles,
                                           notYetProcessed);
                                       FileHandling.TrimFileListOfProcessedFile(this.irgendeinBotListOfProcessedFiles, 50);
                                   }
                                   else
                                   {
                                       this.logger.LogError("Timeout in IrgendeinBotTaskAsync. Don't just stand there, kill something!");
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
        /// <returns></returns>
        private async Task SendYoutubeMetaFileInfoToTelegramChatAsync(List<string> notYetProcessedFiles, TelegramBot theBot, Chat theChat)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   foreach (var newNotProcessedFile in notYetProcessedFiles)
                                   {
                                       if (File.Exists(newNotProcessedFile))
                                       {
                                           var listOfMetaVideoDate = VideoMetaDataFull.DeserializeFromFile(newNotProcessedFile);

                                           // Minimum timeout plus the amount of videos found in file newNotProcessedFile
                                           var timeOut = 5 + listOfMetaVideoDate.Count;

                                           // This construct ensures that its called parallel and the end of the task when sending is completed.
                                           // This seems to happen very quickly and therefore apparently requires a higher timeout.
                                           this.logger.LogDebug(
                                               $"Bot {theBot.Name} start sending async to chat {theChat.ChatName} with id {theChat.ChatId}. TimeOut is 5 + amount of videos: {timeOut}");
                                           var tasks = listOfMetaVideoDate.Select(videoMetaDataFull =>
                                                                                      theBot.SendToChatAsync(
                                                                                          theChat,
                                                                                          videoMetaDataFull.GetReadableDescription(),
                                                                                          timeOut)).ToArray();
                                           Task.WaitAll(tasks);
                                           this.logger.LogDebug($"Bot {theBot.Name} stop sending to chat {theChat.ChatName}");
                                       }
                                       else
                                       {
                                           this.logger.LogError($"File {newNotProcessedFile} not found");
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
        /// Send a message tot the debug channel.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendDebubMessageAsync(string message)
        {
            try
            {
                await this.irgendeinBot.SendToChatAsync(this.debugChannel, message, TimeSpan.FromSeconds(10).Seconds);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }
    }
}