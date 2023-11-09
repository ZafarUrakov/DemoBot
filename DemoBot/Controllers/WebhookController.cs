using DemoBot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DemoBot.Controllers
{
    public class WebhookController : Controller
    {
        [HttpPost]
        public async ValueTask<IActionResult> Post(
            [FromServices] TelegramBotService telegramBotService,
            [FromBody] Update update)
        {
            await telegramBotService.EchoAsync(update);

            return Ok();
        }
    }
}
