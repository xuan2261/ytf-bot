using BotService;
using SimpleLogger;

Logger myLogger = new Logger();
myLogger.LogInfo("Hello, World! I'm the ytf-bot.");

var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");



//var privateBotApiToken = botConfig.Telegram.Bots[1].BotToken;

//TelegramApi.TelegramBot telegramApi = new TelegramApi.TelegramBot(privateBotApiToken, myLogger);
//telegramApi.SendToChat(botConfig.Telegram.Chats[0].ChatId, "My ass hello", 5);

var youtubeApi = new YoutubeApi.YoutubeApi("thyTopBot", botConfig.Youtube.ApiKey, myLogger);

youtubeApi.ReadChannelList(botConfig.Youtube.Channels[0].ChannelId);



Console.ReadKey();
