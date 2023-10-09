// See https://aka.ms/new-console-template for more information

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

Console.WriteLine("Hello, World!");
using CancellationTokenSource cts = new ();

Enumerable.Range(0, 10).ToList().ForEach(x =>
{
    var verifyCode = new Random().Next(0, 9999);
    Console.WriteLine(verifyCode.ToString("0000"));
});


var ReceiverMessage = Task.Run(async () =>
    {
        var botClient = new TelegramBotClient("6593045230:AAFYL4Nnj4JOan3NRp128Mq5j4maqfIpZQY");
        ReceiverOptions receiverOptions = new ()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
    }
    );

var SenderMessage = Task.Run(async () =>
{
    var botClient = new TelegramBotClient("6593045230:AAFYL4Nnj4JOan3NRp128Mq5j4maqfIpZQY");
    ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };

    while (!cts.IsCancellationRequested)
    {
        await botClient.SendTextMessageAsync(
            chatId: 485634926,
            text: $"Вам отправили собщение");
        await Task.Delay(5000);
    }
});

Task.WaitAll(ReceiverMessage, SenderMessage);
    
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "You said:\n" + messageText,
        cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}