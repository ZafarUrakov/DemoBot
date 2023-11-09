using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace DemoBot.Services
{
    public class TelegramBotService
    {
        private readonly ILogger<TelegramBotService> logger;
        private readonly ITelegramBotClient telegramBotClient;

        public TelegramBotService(
            ITelegramBotClient telegramBotClient,
            ILogger<TelegramBotService> logger)
        {
            this.telegramBotClient = telegramBotClient;
            this.logger = logger;
        }

        public async ValueTask EchoAsync(Update update)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageRecieved(update.Message),
                UpdateType.CallbackQuery => BotOnCallBackQueryRecieved(update.CallbackQuery),
                _ => UnknownUpdateTypeHandler(update)
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
            }
        }

        public ValueTask HandleErrorAsync(Exception ex)
        {
            var errorMessage = ex switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error :\n{apiRequestException.ErrorCode}",
                _ => ex.ToString()
            };

            this.logger.LogInformation(errorMessage);

            return ValueTask.CompletedTask;
        }

        private ValueTask UnknownUpdateTypeHandler(Update update)
        {
            this.logger.LogInformation($"Unknown upodate type: {update.Type}");

            return ValueTask.CompletedTask;
        }

        private async ValueTask BotOnCallBackQueryRecieved(CallbackQuery callbackQuery)
        {
            await telegramBotClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"{callbackQuery.Data}");
        }

        private async ValueTask BotOnMessageRecieved(Message message)
        {
            this.logger.LogInformation($"A message has arrieved: {message.Type}");

            await this.telegramBotClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "A message has arrieved to the bot.");
        }
    }
}
