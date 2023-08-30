using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Logic.Handlers.Abstractions;

public abstract class MessageHandlerBase : IMessageHandler
{
    private static Random _r = new Random();

    public abstract int Priority { get; }

    public abstract HandlerMessageType MessageType { get; }

    public abstract Task<RequestBase<Message>?> HandleAsync(Update update, CancellationToken cancellationToken);

    public abstract Task<bool> CanHandleAsync(Update update);


    protected RequestBase<Message> CreateSendStickerMessage(long chatId, string stickerId, int? messagetoReplyId = default, int? threadId = default)
    {
        return new SendStickerRequest(chatId, new InputFileId(stickerId)) { ReplyToMessageId = messagetoReplyId, MessageThreadId = threadId };
    }


    protected RequestBase<Message> CreateRandomStickerMessage(long chatId, string[] stickers, int? messagetoReplyId = default, int? threadId = default)
    {
        string sticker = stickers[_r.Next(stickers.Length)];

        return CreateSendStickerMessage(chatId, sticker, messagetoReplyId, threadId);
    }

    protected RequestBase<Message> CreateTextMessage(long chatId, string text, int? messagetoReplyId = default, int? threadId = default)
    {
        return new SendMessageRequest(chatId, text) { ReplyToMessageId = messagetoReplyId, MessageThreadId = threadId };
    } 
    
    protected RequestBase<Message> CreateDocumentMessage(long chatId, InputFile file)
    {
        return new SendDocumentRequest(chatId, file);
    }


}
