namespace RaiffaisenBot.Logic.Handlers.Abstractions;

public interface IMessageHandler : IUpdateHandler
{
    HandlerMessageType MessageType { get; }
}
