using System.Globalization;
using BotService;
using SimpleLogger;
using YoutubeApi;

Logger myLogger = new Logger();
myLogger.LogInfo($"Hello, World! I'm the ytf-bot. Zulu time is: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");

var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");



//var privateBotApiToken = botConfig.Telegram.Bots[1].BotToken;

//TelegramApi.TelegramBot telegramApi = new TelegramApi.TelegramBot(privateBotApiToken, myLogger);

//telegramApi.SendToChat(botConfig.Telegram.Chats[0].ChatId, "My ass hello", 5);

//var youtubeManager = new YtManager(botConfig.YoutubeConfig.ApiKey, myLogger);

var channelIds = botConfig.YoutubeConfig.Channels.Select(channel => channel.ChannelId).ToList();

var temp = new List<Channel> { botConfig.YoutubeConfig.Channels[0] };
//youtubeManager.StartFullVideoMetaDataWorker(temp);

//youtubeApi.CreateListWithFullVideoMetaDataAsync(channelIds, 5);

Console.WriteLine("Async weiter oder ed?");



Console.WriteLine("Noch result");
Console.ReadKey();
