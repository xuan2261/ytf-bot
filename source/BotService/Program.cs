using BotService;
using Common;
using SimpleLogger;
using TelegramApi;
using YoutubeApi;

Logger myLogger = new Logger("serviceLogfile.log");
myLogger.LogInfo("I bims, ein Service");

var completeServiceConfig = BotConfig.LoadFromJsonFile(@"mybotconfig.json");
var serviceWorkDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceWorkDir");

myLogger.LogInfo("Start Telegram Worker");
var telegramManager = new TelegramManager(completeServiceConfig.TelegramConfig, VideoMetaDataFull.VideoFileSearchPattern, serviceWorkDir);
_ = telegramManager.StartSomeBotToHaufenChat();

myLogger.LogInfo("Start Youtube Worker");
var youtubeApi = new YoutubeApi.YoutubeApi(completeServiceConfig.YoutubeConfig.ApiKey, serviceWorkDir);
var myYoutubeManager = new YtManager(youtubeApi, serviceWorkDir);
_ = myYoutubeManager.StartYoutubeWorker(completeServiceConfig.YoutubeConfig.Channels, 10, MyCallback);

MyCallback("Now", "Startet everything");

Console.WriteLine("Hit e to exit");
Console.WriteLine();
while (Console.ReadKey().Key != ConsoleKey.E)
{
    Console.WriteLine();
    Console.WriteLine("Hit e to exit");
}

telegramManager.StopSomeBotToHaufenChat();
myYoutubeManager.StopYoutubeWorker();

Console.WriteLine("All workers stopped");
Thread.Sleep(TimeSpan.FromSeconds(10));

void MyCallback(string arg1, string message)
{
    _ = telegramManager.SendDebugMessageAsync(arg1 + ": " + message);
}
