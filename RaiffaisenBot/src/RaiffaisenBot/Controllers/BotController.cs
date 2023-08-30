using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RaiffaisenBot.Logic;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok("хуй");
        }
        [HttpPost]
        public async Task<IActionResult> HandleUpdate([FromBody] Update update, [FromServices] ILogger<BotController> logger,
            [FromServices] UpdateService updateService, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(JsonConvert.SerializeObject(update));
            await updateService.HandleUpdateAsync(update, cancellationToken);
            return Ok();
        }
    }
}
