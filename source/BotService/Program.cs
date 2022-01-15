// See https://aka.ms/new-console-template for more information

using BotService;
using Newtonsoft.Json;
using SimpleLogger;
Logger myLogger = new Logger();
myLogger.LogInfo("Hello, World! I'm the ytf-bot.");

var botConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(@"mybotconfig.json"));


Console.ReadKey();
