using RaiffaisenBot.Logic.Handlers.Abstractions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Logic.Handlers.Messages.Text;
public class EchoReplyHandler : MessageHandlerBase
{
    public override int Priority => int.MaxValue;

    public override HandlerMessageType MessageType => HandlerMessageType.Text;

    public override async Task<bool> CanHandleAsync(Update update)
    {
        var message = update.Message;

        if (message == null)
        {
            return false;
        }
        if (MessageType != Mapper.MapToHandlerMessageType(message.Type))
        {
            return false;
        }
        if (string.IsNullOrEmpty(message.Text))
        {
            return false;
        }
        return true;
    }

    public override Task<RequestBase<Message>> HandleAsync(Update update, CancellationToken cancellationToken)
    {
        return Task.FromResult(CreateTextMessage(update.Message!.Chat.Id, update.Message!.Text!));
    }
}
