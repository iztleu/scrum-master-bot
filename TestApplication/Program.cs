// See https://aka.ms/new-console-template for more information

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


using CancellationTokenSource cts = new ();

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
            text: $"<b style=\"color:red\">bold</b>, <strong>bold</strong>" +
                  $"\n<i>italic</i>, <em>italic</em>" +
                  $"\n<u>underline</u>, <ins>underline</ins>" +
                  $"\n<s>strikethrough</s>, <strike>strikethrough</strike>, <del>strikethrough</del>" +
                  $"\n<span class=\"tg-spoiler\">spoiler</span>, <tg-spoiler>spoiler</tg-spoiler>" +
                  $"\n<b>bold <i>italic bold <s>italic bold strikethrough <span class=\"tg-spoiler\">italic bold strikethrough spoiler</span></s> <u>underline italic bold</u></i> bold</b>" +
                  $"\n<a href=\"http://www.example.com/\">inline URL</a>" +
                  $"\n<a href=\"tg://user?id=123456789\">inline mention of a user</a>" +
                  $"\n<tg-emoji emoji-id=\"5368324170671202286\">\ud83d\udc4d</tg-emoji>" +
                  $"\n<code>inline fixed-width code</code>" +
                  $"\n<pre>pre-formatted fixed-width code block</pre>" +
                  $"\n<pre><code class=\"language-python\">pre-formatted fixed-width code block written in the Python programming language</code></pre>",
            parseMode: ParseMode.Html);
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