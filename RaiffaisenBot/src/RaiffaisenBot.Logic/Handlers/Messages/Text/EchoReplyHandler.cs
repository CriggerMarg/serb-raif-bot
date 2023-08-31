using RaiffaisenBot.Logic.Handlers.Abstractions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Logic.Handlers.Messages.Text;
public class EchoReplyHandler : MessageHandlerBase
{
    private const string WelcomeMessage
= @"Welcome to Raiffeisen Bank Statement Bot

We offer a service to convert your Raiffeisen bank account statements from PDF to CSV format for financial analysis.

Disclaimer:
While we aim for accurate conversion, errors may occur. We do not guarantee precision. Use the generated CSV files with caution.

Unofficial Service:
This bot is not affiliated with Raiffeisen Bank. We are an independent service.

Protect Your Privacy:
Before submitting, consider using an online PDF editor to remove personal data: https://www.google.com/search?q=online+pdf+editor

No Data Storage:
We do not store your data. Our source code is available in the bot's bio for examination.

To get started, send your Raiffeisen bank account statement in PDF format. We'll convert it to CSV for your analysis.

If you encounter issues or have questions, feel free to reach out.";
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

    public override async Task<RequestBase<Message>> HandleAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;

        var text = message.Text?.ToLowerInvariant().Trim().Replace("\"", "").Replace("'", "") ?? string.Empty;

        return text switch
        {
            string start when start == "/start" => CreateTextMessage(update.Message!.Chat.Id, WelcomeMessage),
            _ => CreateTextMessage(update.Message!.Chat.Id, update.Message!.Text!)
        };
    }
}
