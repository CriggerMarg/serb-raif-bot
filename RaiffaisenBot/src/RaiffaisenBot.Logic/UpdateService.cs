using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaiffaisenBot.Logic
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly ITelegramBotClient _botClient;
        private readonly UpdateHandlerFactory _handlerFactory;

        public UpdateService(ILogger<UpdateService> logger, ITelegramBotClient botClient, UpdateHandlerFactory handlerFactory)
        {
            _logger = logger;
            _botClient = botClient;
            _handlerFactory = handlerFactory;
        }
        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            try
            {
                var handlers = update.Type switch
                {
                    UpdateType.Message => await _handlerFactory.ResolveHandlerAsync(update),
                    _ => Enumerable.Empty<Handlers.Abstractions.IUpdateHandler>()
                };

                foreach (Handlers.Abstractions.IUpdateHandler handler in handlers)
                {
                    _logger.LogInformation($"Executing handler {handler.GetType().Name}");
                    if (await handler.CanHandleAsync(update))
                    {
                        _logger.LogInformation($"Handler {handler.GetType().Name} able to handle request");
                        var request = await handler.HandleAsync(update, cancellationToken);
                        if (request != null)
                        {
                            await _botClient.MakeRequestAsync(request, cancellationToken);
                        }
                    }
                }
            }
            catch (NotImplementedException e)
            {
                _logger.LogError(e, "пока не готово");
                // нормально
            }
        }
    }
}
