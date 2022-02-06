using Common;
using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        /// <summary>
        /// Path to the file in which the names of the already processed YoutubeMeta files are located.
        /// </summary>
        public static string PathToListOfProcessedFiles => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TelegramProcessedFiles.list");

        private readonly Logger logger;
        private bool workerShallRun;

        // ReSharper disable once InconsistentNaming
        private readonly TelegramBot irgendeinBot, unh317_01, blackmetaloid;
        private readonly Chat haufen, gBm, bM;

        private readonly string workingPath;
        private readonly string irgendeinBotListOfProcessedFiles;
        private readonly string youtubeSearchPattern;

        /// <summary>
        /// Ctor.
        /// </summary>
        public TelegramManager(TelegramConfig myConfig, string workingPath, string youtubeSearchPattern)
        {
            var unh31701Config = myConfig.Bots[0];
            var irgendeinBotConfig = myConfig.Bots[1];
            var blackmetaloidConfig = myConfig.Bots[2];

            this.haufen = myConfig.Chats[0];
            this.gBm = myConfig.Chats[1];
            this.bM = myConfig.Chats[2];

            this.logger = new Logger("TelegramManager.log");

            this.irgendeinBot = new TelegramBot(irgendeinBotConfig.BotToken, irgendeinBotConfig.BotName);
            this.blackmetaloid = new TelegramBot(blackmetaloidConfig.BotToken, blackmetaloidConfig.BotName);
            this.unh317_01 = new TelegramBot(unh31701Config.BotToken, unh31701Config.BotName);

            this.youtubeSearchPattern = youtubeSearchPattern;

            this.workingPath = workingPath;
            this.irgendeinBotListOfProcessedFiles = Path.Combine(workingPath, "irgendeinBot.list");
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
            await Task.Run(() =>
                           {
                               while (this.workerShallRun)
                               {
                                   _ = IrgendeinBotWorker();
                                   Thread.Sleep(TimeSpan.FromSeconds(30));
                               }
                           });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task IrgendeinBotWorker()
        {
            try
            {
                await Task.Run(() =>
                               {
                                   // This has to be synchronised because it is a coherent process and the individual steps are interdependent.
                                   // It is probably not necessary to secure this process with a mutex, because each bot must manage
                                   // its own list of already processed files.
                                   var notYetProcessed = FileHandling.FindNotYetProcessedYoutubeMetaFiles(this.irgendeinBotListOfProcessedFiles,
                                                                                                          this.workingPath,
                                                                                                          this.youtubeSearchPattern);

                                   if (SendYoutubeMetaFileInfoToTelegramChat(notYetProcessed, this.irgendeinBot, this.haufen)
                                       .Wait(TimeSpan.FromSeconds(10)))
                                   {
                                       FileHandling.WriteProcessedFileNamesIntoListOfProcessedFiles(this.irgendeinBotListOfProcessedFiles, 
                                                                                                    notYetProcessed);
                                       FileHandling.TrimFileListOfProcessedFile(this.irgendeinBotListOfProcessedFiles, 50);
                                   }
                                   else
                                   {
                                       this.logger.LogError("Timeout in IrgendeinBotWorker. Don't just stand there, kill something!");
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
        private async Task SendYoutubeMetaFileInfoToTelegramChat(List<string> notYetProcessedFiles, TelegramBot theBot, Chat theChat)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   foreach (var newProcessedFile in notYetProcessedFiles)
                                   {
                                       if (File.Exists(newProcessedFile))
                                       {
                                           var listOfMetaVideoDate = VideoMetaDataFull.DeserializeFromFile(newProcessedFile);

                                           // This construct ensures that its called parallel and the end of the task when sending is completed.
                                           var tasks = listOfMetaVideoDate.Select(videoMetaDataFull =>
                                                                                      theBot.SendToChatAsync(
                                                                                          theChat.ChatId,
                                                                                          videoMetaDataFull.GetReadableDescription(),
                                                                                          5)).ToArray();
                                           Task.WaitAll(tasks);
                                       }
                                       else
                                       {
                                           this.logger.LogError($"File {newProcessedFile} not found");
                                       }
                                   }
                               });
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
            }
        }
    }
}