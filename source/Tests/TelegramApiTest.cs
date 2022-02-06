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

       
        /// <summary>
        /// Keine Ahnung warum man hier DeploymentItems verwenden soll. Der Scheiß funktioniert ja eh nicht richtig. Bei Verwendung der Attribute
        /// ClassInitialize und ClassCleanup.
        /// </summary>
        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json")]
        [DeploymentItem("TelegramProcessedFiles.list")]
        [TestMethod]
        public void TestGetFiles()
        {

            var theList = TelegramManager.FindNotYetProcessedYoutubeMetaFiles(TelegramManager.PathToListOfProcessedFiles, 
                                                                              searchPattern: "Full_Meta_YT.json");
            Assert.AreEqual(theList.Count,1);
            CleanupFullMetaYTFiles();
        }


        /// <summary>
        /// CleanUp, Initialize and DeploymentItem do not work as expected. MS sucks, no exceptions. Therefore a separate method.
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
                .Where(file => file.EndsWith(TelegramManager.PathToListOfProcessedFiles))
                .ToList()
                .ForEach(File.Delete);
        }

        [TestMethod]
        public void SendTwoFullDescriptionsIntoChat()
        {
            var listOfFilenames = new List<string>();
            listOfFilenames.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2022-01-15T09-09-55Z_Full_Meta_YT.json"));
            listOfFilenames.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2022-01-23T13-10-11Z_Full_Meta_YT.json"));

            var manager = new TelegramManager(BotConfig.LoadFromJsonFile(@"mybotconfig.json").TelegramConfig);
            manager.TaskForirgendeinBot(listOfFilenames).Wait();

            Thread.Sleep(TimeSpan.FromSeconds(20));
            Console.WriteLine();
        }
    }
}