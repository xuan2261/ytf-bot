using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TelegramApi;

namespace Tests
{
    [TestClass]
    public class TelegramApiTest
    {
        [TestMethod]
        public void TestLoadConfig()
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");

            var myTelegramManager = new TelegramManager(botConfig.TelegramConfig);
        }

        [TestMethod]
        public void TestSendMessage()
        {
            var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");

            var myTelegramManager = new TelegramBot(botConfig.TelegramConfig.Bots[1].BotToken);
            myTelegramManager.SendToChatAsync(botConfig.TelegramConfig.Chats[0].ChatId, "I bims von de Tests.", 10);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
