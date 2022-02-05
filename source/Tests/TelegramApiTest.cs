using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            var myTelegramBot = new TelegramBot(theBot.BotToken);
            myTelegramBot.SendToChatAsync(theChat.ChatId, "Hallo Elki, Oli hat dich ganz arg lieb.", 10);
            Thread.Sleep(TimeSpan.FromSeconds(20));
            myTelegramBot.SendToChatAsync(theChat.ChatId, "Mal 10 :-)", 10);
            for (int i = 2; i <= 10; i++)
            {
                myTelegramBot.SendToChatAsync(theChat.ChatId, $"Hallo Elki, Oli hat dich ganz arg lieb. {i:D2}", 10);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

       
        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json")]
        [DeploymentItem("TelegramProcessedFiles.list")]
        [TestMethod]
        public void TestGetFiles()
        {
            var myManager = new TelegramManager(BotConfig.LoadFromJsonFile(@"mybotconfig.json").TelegramConfig);
            var theList = myManager.FindNotYetProcessedFiles(TelegramManager.TelegramListOfProcessedFiles, searchPattern: "Full_Meta_YT.json");

            Assert.AreEqual(theList.Count,2);

            CleanupFullMetaYTFiles();
        }


        /// <summary>
        /// Runs after every test to cleanup bi folder.
        /// </summary>
        
        public void CleanupFullMetaYTFiles()
        {
            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith("Full_Meta_YT.json"))
                .ToList()
                .ForEach(File.Delete);

            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith(TelegramManager.TelegramListOfProcessedFiles))
                .ToList()
                .ForEach(File.Delete);
        }
    }
}