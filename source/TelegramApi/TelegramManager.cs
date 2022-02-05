using System.Runtime.CompilerServices;
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

        private readonly Chat haufen, gBm, bM;

        // ReSharper disable once InconsistentNaming
        private readonly TelegramBot irgendeinBot, unh317_01, blackmetaloid;

        public TelegramManager(TelegramConfig myConfig)
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
                                   Thread.Sleep(TimeSpan.FromSeconds(30));
                               }
                           });
        }

        /// <summary>
        /// Method finds the youtube meta video files that are not yet processed.
        /// Files with a freely definable content exist in the current directory. The file names of these files end with
        /// "searchPattern". It is assumed that these files are somehow processed and the already processed files are located as a
        /// string in the text file "pathToListOfProcessedFileNames".This method creates a list of file names that have not yet been
        /// processed and are therefore not in the file "pathToListOfProcessedFileNames".
        /// </summary>
        /// <returns>Returns a list within the full path to youtube meta files that are not processed yet.</returns>
        public static List<string> FindNotYetProcessedYoutubeMetaFiles(string pathToListOfProcessedFileNames, string searchPattern)
        {
            var availableYoutubeMetaFiles = Directory
                                            .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                                            .Where(file => file.EndsWith(searchPattern))
                                            .ToList();
            var notYetProcessedFileNames = new List<string>();
            if (File.Exists(pathToListOfProcessedFileNames))
            {
                var listOfProcessedYoutubeFiles = File.ReadAllLines(pathToListOfProcessedFileNames).ToList();
                availableYoutubeMetaFiles.ForEach(fullPathToFile =>
                                                  {
                                                      if (!listOfProcessedYoutubeFiles.Contains(Path.GetFileName(fullPathToFile)))
                                                      {
                                                          notYetProcessedFileNames.Add(fullPathToFile);
                                                      }
                                                  });
            }
            else
            {
                // If there is not yet a file that logs which youtube meta files have been processed, a list of the names of all the
                // youtube meta files that can be found is returned.
                return availableYoutubeMetaFiles;
            }
            return notYetProcessedFileNames;
        }

        /// <summary>
        /// This method writes the list of the names of the files that have been processed to the log file pathToListOfProcessedFiles.
        /// </summary>
        /// <param name="pathToListOfProcessedFiles"></param>
        /// <param name="newProcessedFiles"></param>
        public static void WriteProcessedFileNamesIntoListOfProcessedFiles(string pathToListOfProcessedFiles, List<string> newProcessedFiles)
        {
            File.AppendAllLines(pathToListOfProcessedFiles, newProcessedFiles);
        }

        public async Task TaskForirgendeinBot(List<string> newProcessedFiles)
        {
            await SendYoutubeMetaFileInfoToTelegramChat(newProcessedFiles, this.irgendeinBot, this.haufen);
        }

        private async Task SendYoutubeMetaFileInfoToTelegramChat(List<string> newProcessedFiles, TelegramBot theBot, Chat theChat)
        {
            try
            {
                await Task.Run(() =>
                               {
                                   foreach (var newProcessedFile in newProcessedFiles)
                                   {
                                       if (File.Exists(newProcessedFile))
                                       {
                                           var listOfMetaVideoDate = VideoMetaDataFull.DeserializeFromFile(newProcessedFile);
                                           foreach (var videoMetaDataFull in listOfMetaVideoDate)
                                           {
                                               theBot.SendToChatAsync(theChat.ChatId, videoMetaDataFull.GetReadableDescription(), 5);
                                           }
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