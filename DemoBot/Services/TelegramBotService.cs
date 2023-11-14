using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using static System.Net.Mime.MediaTypeNames;
using DemoBot.Models;

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

            if(message.Text is not null)
            {
                if(IsPhoneNumber((message.Text)))
                {
                    await this.telegramBotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Thank you {message.Chat.FirstName}, you will receive a progress report.");

                    Student student = new Student
                    {
                        Id = Guid.NewGuid(),
                        PhoneNumber = message.Text,
                        TelegramId = message.Chat.Id
                    };
                }
                else
                {
                    await this.telegramBotClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Welcome {message.Chat.FirstName} {message.Chat.LastName}, please send us the phone number.");
                }
            }


        }

        private bool IsPhoneNumber(string text)
        {
            if (text.StartsWith("+") || long.TryParse(text, out _))
            {
                return true; 
            }

            return false; 
        }
    }
}
