﻿using BotService;
using SimpleLogger;
using YoutubeApi;

Logger myLogger = new Logger();
myLogger.LogInfo("Hello, World! I'm the ytf-bot.");

var botConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");



//var privateBotApiToken = botConfig.Telegram.Bots[1].BotToken;

//TelegramApi.TelegramBot telegramApi = new TelegramApi.TelegramBot(privateBotApiToken, myLogger);

//telegramApi.SendToChat(botConfig.Telegram.Chats[0].ChatId, "My ass hello", 5);

var youtubeManager = new YtManager(botConfig.Youtube.ApiKey, myLogger);

var channelIds = botConfig.Youtube.Channels.Select(channel => channel.ChannelId).ToList();


//youtubeApi.CreateVideoFile(channelIds, 5);

Console.WriteLine("Async weiter oder ed?");



Console.WriteLine("Noch result");
Console.ReadKey();
