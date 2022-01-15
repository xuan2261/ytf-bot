// See https://aka.ms/new-console-template for more information

using BotService;
using Newtonsoft.Json;
using SimpleLogger;

Logger myLogger = new Logger();
myLogger.LogInfo("Hello, World! I'm the ytf-bot.");

var botConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(@"mybotconfig.json"));


if (botConfig.Telegram != null)
{
    var privateBotApiToken = botConfig.Telegram.Bots?[1].BotToken;
    if (privateBotApiToken != null)
    {
        TelegramApi.TelegramApi telegramApi = new TelegramApi.TelegramApi(privateBotApiToken, myLogger);
        telegramApi.SendToChat(botConfig.Telegram.Chats[0].ChatId.Value, "My ass hello", 5);
    }
}


Console.ReadKey();
