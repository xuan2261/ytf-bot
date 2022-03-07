using SimpleLogger;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramApi
{
    /// <summary>
    /// Class provides functionality to access to telegram chats and send messages.
    /// One instance per bot is needed, so this api may may be more a bot client wrapper.
    ///
    /// Understand that you need to add your bot to chats and groups for sending messages.
    /// </summary>
    public class TelegramBot
    {
        private readonly Logger logger;
        private readonly TelegramBotClient telegramBotClient;
        public readonly string Name;

        /// <summary>
        /// Ctor.
        /// Creates a bit client with your private token
        /// </summary>
        /// <param name="privateBotApiToken"></param>
        /// <param name="name">Name of the bot</param>
        /// <param name="theLogger"></param>
        public TelegramBot(string privateBotApiToken, string name, Logger? theLogger = null)
        {
            this.logger = theLogger ?? new Logger($"{name}.log");
            this.Name = name;
            try
            {
                this.telegramBotClient = new TelegramBotClient(privateBotApiToken);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Async method to send message to telegram chat or user.
        /// Method is fire and forget, errors are logged on console or in file.
        /// </summary>
        /// <param name="theChat">Chat to send a message in.</param>
        /// <param name="messageToPublish">The message to publish.</param>
        /// <param name="timeOut">After timeOut seconds an error is logged and there was no message sent.</param>
        public async Task SendToChatAsync(Chat theChat, string messageToPublish, int timeOut)
        {
            
            await Task.Run(() =>
                           {
                               try
                               {
                                   var shortMessage = messageToPublish;
                                   if (messageToPublish.Length > 29) shortMessage = shortMessage.Substring(0, 29);

                                   var cancellationTokenSource = new CancellationTokenSource();
                                   var autoResetForSendMessage = new AutoResetEvent(false);

                                   // this.logger.LogDebug($"Start sending to chat {theChat.ChatName}. Message: {shortMessage}");
                                   _ = InternalSendMessageToChatAsync(theChat.ChatId, 
                                                                      messageToPublish, 
                                                                      cancellationTokenSource.Token, 
                                                                      autoResetForSendMessage);

                                   if (!autoResetForSendMessage.WaitOne(TimeSpan.FromSeconds(timeOut)))
                                   {
                                       this.logger.LogError($"TimeOut sending to chat {theChat.ChatName}. Message: {shortMessage}...");
                                       cancellationTokenSource.Cancel();
                                   }
                                   else
                                   {
                                       this.logger.LogInfo($"Success sending to chat {theChat.ChatName}. Message: {shortMessage}...");
                                   }
                               }
                               catch (Exception e)
                               {
                                   this.logger.LogError(e.Message);
                                   throw;
                               }
                           });
        }

        /// <summary>
        /// Internal helper to send/publish a message in a chat/group/channel.
        /// The Telegram Api documentation is unfortunately very imprecise. It is also not at all clear what is in 'message' if publishing
        /// did not work. 
        /// </summary>
        private async Task InternalSendMessageToChatAsync(long chatId, string messageToPublish, CancellationToken cts, AutoResetEvent myAutoReset)
        {
            var sentMessage = await this.telegramBotClient.SendTextMessageAsync(
                                  chatId: new ChatId(chatId),
                                  text: messageToPublish,
                                  cancellationToken: cts);

            if (sentMessage.Text == null)
            {
                this.logger.LogError("TelegramApi method 'SendTextMessageAsync' returned null. No message was sent to chat");
            }

            myAutoReset.Set();
        }
    }
}