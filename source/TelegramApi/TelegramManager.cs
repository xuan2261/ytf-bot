using Common;
using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        private readonly Logger logger;
        private bool someBotToHaufenChatWorkerShallRun;

        private readonly TelegramBot irgendeinBot, unh317_01Bot, blackmetaloidBot;
        private readonly Chat haufenChat, gBmChat, bMChat, debugChannel;

        private readonly string irgendeinBotListOfProcessedFiles;
        private readonly string blackmetaloidBotListOfProcessedFiles;
        private readonly string unh317_01BotListOfProcessedFiles;

        /// <summary>
        /// The WorkDir contains all the files needed for the TelegramManager:
        /// 1.  List of files within VideoMetaDataFull per video to check which videos have been published on the channels.
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
            this.unh317_01BotListOfProcessedFiles = Path.Combine(this.WorkDir, $"__{this.unh317_01Bot.Name}.list");

            this.logger.LogDebug("Ctor successful");
        }

        /// <summary>
        /// This method stops the internal worker.
        /// No channel will be read after that and the object has to be destroyed.
        /// </summary>
        public void StopSomeBotToHaufenChat()
        {
            this.someBotToHaufenChatWorkerShallRun = false;
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
                                   if (!SomeBotToHaufenChatTaskAsync().Wait(TimeSpan.FromSeconds(45)))
                                   {
                                       this.logger.LogWarning("TimeOut in IrgendeinBotTask async. Check it.");
                                       _ = SendDebugMessageAsync("TimeOut in IrgendeinBotTask async. Check it.");
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
                                   var notYetProcessed = FileHandling.FindNotYetProcessedVideoIdFiles(this.blackmetaloidBotListOfProcessedFiles,
                                       this.WorkDir,
                                       VideoMetaDataFull.VideoFileSearchPattern);

                                   if (SendYoutubeMetaFileInfoToTelegramChatAsync(notYetProcessed, this.blackmetaloidBot, this.bMChat)
                                       .Wait(TimeSpan.FromSeconds(10)))
                                   {
                                       FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(this.blackmetaloidBotListOfProcessedFiles,
                                           notYetProcessed);
                                       FileHandling.TrimFileListOfProcessedFile(this.blackmetaloidBotListOfProcessedFiles, 50);
                                   }
                                   else
                                   {
                                       this.logger.LogError("Timeout in SomeBotToHaufenChatTaskAsync. Don't just stand there, kill something!");
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
        public async Task SomeBotToHaufenChatTaskAsync()
        {
            try
            {
                await Task.Run(() =>
                               {
                                   // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                                   // It is probably not necessary to secure this process with a mutex, because each bot must manage
                                   // its own list of already processed files.
                                   var notYetProcessed = 
                                       FileHandling.FindNotYetProcessedVideoIdFiles(this.irgendeinBotListOfProcessedFiles,
                                                                                        this.WorkDir,
                                                                                        VideoMetaDataFull.VideoFileSearchPattern);

                                   if (SendYoutubeMetaFileInfoToTelegramChatAsync(notYetProcessed, this.irgendeinBot, this.haufenChat)
                                       .Wait(TimeSpan.FromSeconds(5+notYetProcessed.Count*2)))
                                   {
                                       FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(this.irgendeinBotListOfProcessedFiles,
                                                                                                    notYetProcessed);
                                       FileHandling.TrimFileListOfProcessedFile(this.irgendeinBotListOfProcessedFiles, 200);
                                   }
                                   else
                                   {
                                       this.logger.LogError("Timeout in SomeBotToHaufenChatTaskAsync. Don't just stand there, kill something!");
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
                                   List<VideoMetaDataFull> listOfMetaVideoDate = new List<VideoMetaDataFull>();
                                   notYetProcessedFiles.ForEach(file =>
                                                                {
                                                                    if (File.Exists(file))
                                                                    {
                                                                        listOfMetaVideoDate.Add(VideoMetaDataFull.DeserializeFromFile(file));
                                                                    }
                                                                    else
                                                                    {
                                                                        this.logger.LogError($"File {file} not found");
                                                                    }
                                                                });

                                   if (listOfMetaVideoDate.Count > 0)
                                   {
                                       // Minimum timeout plus the amount of videos found in file newNotProcessedFile
                                       var timeOut = 5 + listOfMetaVideoDate.Count * 2;
                                       this.logger.LogDebug(
                                           $"Bot {theBot.Name} start sending to {theChat.ChatName} with id {theChat.ChatId}. " +
                                           $"Timeout {timeOut} seconds. Videos {listOfMetaVideoDate.Count}");

                                       var tasks = listOfMetaVideoDate.Select(videoMetaDataFull =>
                                                                                  theBot.SendToChatAsync(
                                                                                      theChat,
                                                                                      videoMetaDataFull.GetReadableDescription(),
                                                                                      timeOut)).ToArray();
                                       Task.WaitAll(tasks);
                                       this.logger.LogInfo($"Bot {theBot.Name} sent to chat {theChat.ChatName} {listOfMetaVideoDate.Count} videos.");
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
                await this.irgendeinBot.SendToChatAsync(this.debugChannel, message, TimeSpan.FromSeconds(10).Seconds);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }
    }
}