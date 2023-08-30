using RaiffaisenBot.Logic.Handlers;
using RaiffaisenBot.Logic.Handlers.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaiffaisenBot.Logic;

public class UpdateHandlerFactory 
{
    private readonly Func<HandlerMessageType, IEnumerable<IMessageHandler>> _messageHandlerFactory;

    public UpdateHandlerFactory(Func<HandlerMessageType, IOrderedEnumerable<IMessageHandler>> messageHandlerFactory)
    {
        _messageHandlerFactory = messageHandlerFactory;
    }

    public async Task<IOrderedEnumerable<IUpdateHandler>> ResolveHandlerAsync(Update update)
    {
        var updateType = update.Type;
        var handlers = updateType switch
        {
            UpdateType.Message => _messageHandlerFactory(Mapper.MapToHandlerMessageType(update.Message!.Type)),
            //UpdateType.ChatMember => await _chatMemberHandlerFactory.ResolveHandlerAsync(update.ChatMember),

            // Add more UpdateType cases here
            _ => throw new NotImplementedException($"Handler for UpdateType '{update}' is not supported."),
        };

        return (IOrderedEnumerable<IUpdateHandler>)handlers.Cast<IUpdateHandler>();
    }
}