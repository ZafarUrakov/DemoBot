using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;
using DemoBot.Models;

namespace DemoBot.Services
{
    public class ConfigureWebhook : IHostedService
    {
        private readonly ILogger<ConfigureWebhook> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly BotConfiguration botConfiguration;
        public ConfigureWebhook(
            ILogger<ConfigureWebhook> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.botConfiguration = configuration
                .GetSection("BotConfiguration").Get<BotConfiguration>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = this.serviceProvider.CreateScope();

            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            var webhookAddress = $@"{botConfiguration.HostAdress}/bot/{botConfiguration.Token}";

            this.logger.LogInformation("Setting webhook");

            await botClient.SendTextMessageAsync(
                chatId: 1924521160,
                text: "Webhook starting work..");

            await botClient.SetWebhookAsync(
                    url: webhookAddress,
                    allowedUpdates: Array.Empty<UpdateType>(),
                    cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = this.serviceProvider.CreateScope();

            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            this.logger.LogInformation("Setting webhook");

            await botClient.SendTextMessageAsync(
                chatId: 1924521160,
                text: "Bot sleeping");
        }
    }
}
