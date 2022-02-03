using SimpleLogger;

namespace TelegramApi
{
    public class TelegramManager
    {
        private readonly Logger logger;
        private bool workerShallRun;

        private Bot myPlayBot, bmBot, unh317_01;
        private Chat haufen, gBM, bM;

        public TelegramManager(TelegramConfig myConfig)
        {
            this.logger = new Logger("TelegramManager.log");


            LoadConfiguration(myConfig);
        }

        public void LoadConfiguration(TelegramConfig myConfig)
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

        
        public async Task StartYoutubeWorkerWorker()
        {
            await Task.Run(() =>
                           {
                               while (this.workerShallRun)
                               {

                                   Thread.Sleep(TimeSpan.FromSeconds(30));
                               }
                           });
        }
    }
}