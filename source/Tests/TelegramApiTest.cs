using BotService;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TelegramApi;

namespace Tests
{
    [TestClass]
    public class TelegramApiTest
    {
        /// <summary>
        /// Helper method for creating the testbot.
        /// </summary>
        /// <param name="theChat"></param>
        /// <param name="theBot"></param>
        private void MakeBot(out Chat theChat, out Bot theBot)
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
            theBot = botConfig.TelegramConfig.Bots[1];
            theChat = botConfig.TelegramConfig.Chats[0];
        }

        /// <summary>
        /// Method just send some test messages into a channel.
        /// </summary>
        [TestMethod]
        public void TestSendMessage()
        {
            MakeBot(out var theChat, out var theBot);

            var myTelegramBot = new TelegramBot(theBot.BotToken, theBot.BotName);
            myTelegramBot.SendToChatAsync(theChat.ChatId, "Hallo Elki, Oli hat dich ganz arg lieb.", 10);
            Thread.Sleep(TimeSpan.FromSeconds(20));
            myTelegramBot.SendToChatAsync(theChat.ChatId, "Mal 10 :-)", 10);
            for (int i = 2; i <= 10; i++)
            {
                myTelegramBot.SendToChatAsync(theChat.ChatId, $"Hallo Elki, Oli hat dich ganz arg lieb. {i:D2}", 10);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }



        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json", "work")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json", "work")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json", "work")]
        [TestMethod]
        public void SendDescriptionsOfTwoFullMetaYtFilesIntoChat()
        {
            var telegramConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json").TelegramConfig;
            var workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            var manager = new TelegramManager(telegramConfig, workDir, VideoMetaDataFull.YoutubeSearchPattern);

            for (int i = 0; i < 10; i++)
            {
                _ = manager.IrgendeinBotTaskAsync();
                Console.WriteLine($"Startet {i}");
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            Console.WriteLine();


            // This is not a unit test. Check your chat to verify if this construct did work.
        }
    }
}