using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Logic.Handlers.Abstractions;

public interface IUpdateHandler
{
    int Priority { get; }

    Task<bool> CanHandleAsync(Update update);

    /// <summary>
    /// Return null if there is no need to send an request back
    /// </summary>
    /// <param name="update"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<RequestBase<Message>?> HandleAsync(Update update, CancellationToken cancellationToken);
}
