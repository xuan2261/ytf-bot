using BotService;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using TelegramApi;

namespace Tests
{
    [TestClass]
    public class TelegramTests
    {
        public string WorkFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testTelegramWorkDir");

        /// <summary>
        /// Helper method for creating the testbot.
        /// </summary>
        /// <param name="haufenChat"></param>
        /// <param name="irgendeinBot"></param>
        private void MakeBot(out Chat haufenChat, out Bot irgendeinBot)
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            irgendeinBot = botConfig.TelegramConfig.Bots[1];
            haufenChat = botConfig.TelegramConfig.Chats[0];
        }

        /// <summary>
        /// Method just send some test messages into a channel.
        /// </summary>
        [TestMethod]
        public void TestSendMessage()
        {
            MakeBot(out var haufenChat, out var irgendeinBot);

            var myTelegramBot = new TelegramBot(irgendeinBot.BotToken, irgendeinBot.BotName);
            _ = myTelegramBot.SendToChatAsync(haufenChat.ChatId, "Hallo Elki, Oli hat dich ganz arg lieb.", 10);
            Thread.Sleep(TimeSpan.FromSeconds(20));
            _ = myTelegramBot.SendToChatAsync(haufenChat.ChatId, "Mal 10 :-)", 10);
            for (int i = 2; i <= 10; i++)
            {
                _ = myTelegramBot.SendToChatAsync(haufenChat.ChatId, $"Hallo Elki, Oli hat dich ganz arg lieb. {i:D2}", 10);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }



        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json", "testTelegramWorkDir")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json", "testTelegramWorkDir")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json", "testTelegramWorkDir")]
        [TestMethod]
        public void SendDescriptionsOfTwoFullMetaYtFilesIntoChat()
        {
            var logger = new SimpleLogger.Logger();
            var telegramConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").TelegramConfig;
            var manager = new TelegramManager(telegramConfig, VideoMetaDataFull.YoutubeSearchPattern, WorkFolder);

            for (int i = 0; i < 10; i++)
            {
                if (!manager.IrgendeinBotTaskAsync().Wait(TimeSpan.FromSeconds(10)))
                {
                    logger.LogWarning("There may be a problem in the telegram test. Timeout.");
                }
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }

            Console.WriteLine();
            // This is not a unit test. Check your chat to verify if this construct did work.
        }
    }
}