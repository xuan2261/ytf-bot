using System.Globalization;
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
var telegramManager = new TelegramManager(completeServiceConfig.TelegramConfig, VideoMetaDataFull.YoutubeSearchPattern, serviceWorkDir);
_ = telegramManager.StartTelegramWorker();

myLogger.LogInfo("Start Youtube Worker");
var youtubeApi = new YoutubeApi.YoutubeApi("IrrelevantApplicationName", completeServiceConfig.YoutubeConfig.ApiKey);
var myYoutubeManager = new YtManager(youtubeApi, serviceWorkDir);
_ = myYoutubeManager.StartYoutubeWorker(completeServiceConfig.YoutubeConfig.Channels, MyCallback);

MyCallback("Now", "Startet everything");

Console.WriteLine("Hit e to exit");
Console.WriteLine();
while (Console.ReadKey().Key != ConsoleKey.E)
{
    Console.WriteLine();
    Console.WriteLine("Hit e to exit");
}

telegramManager.StopYoutubeWorker();
myYoutubeManager.StopYoutubeWorker();

Console.WriteLine("All workers stopped");
Thread.Sleep(TimeSpan.FromSeconds(10));

void MyCallback(string timeStamp, string message)
{
    _ = telegramManager.SendDebubMessageAsync(timeStamp + ": " + message);
}
