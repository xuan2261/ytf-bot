using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        public static string TelegramListOfProcessedFiles => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TelegramProcessedFiles.list");

        private readonly Logger logger;
        private bool workerShallRun;

        private Bot myPlayBot, bmBot, unh317_01;
        private Chat haufen, gBM, bM;

        public TelegramManager(TelegramConfig myConfig)
        {
            this.logger = new Logger("TelegramManager.log");
            LoadConfiguration(myConfig);
        }

        private void LoadConfiguration(TelegramConfig myConfig)
        {
            this.unh317_01 = myConfig.Bots[0];
            this.myPlayBot = myConfig.Bots[1];
            this.bmBot = myConfig.Bots[2];

            this.haufen = myConfig.Chats[0];
            this.gBM = myConfig.Chats[1];
            this.bM = myConfig.Chats[2];
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
        /// string in the text file "fileWithProcessedFiles".This method creates a list of file names that have not yet been
        /// processed and are therefore not in the file "fileWithProcessedFiles".
        /// </summary>
        /// <returns>Returns a list with file names that are not processed yet.</returns>
        public List<string> FindNotYetProcessedFiles(string fileWithProcessedFiles, string searchPattern)
        {
            var availableYoutubeMetaFiles = Directory
                                            .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                                            .Where(file => file.EndsWith(searchPattern))
                                            .ToList();
            var result = new List<string>();
            if (File.Exists(TelegramListOfProcessedFiles))
            {
                var listOfProcessedYoutubeFiles = File.ReadAllLines(fileWithProcessedFiles).ToList();
                availableYoutubeMetaFiles.ForEach(file =>
                                                  {
                                                      if (listOfProcessedYoutubeFiles.Contains(Path.GetFileName(file)))
                                                      {
                                                          result.Add(file);
                                                      }
                                                  });
            }
            else
            {
                return availableYoutubeMetaFiles;
            }

            return result;
        }
    }
}