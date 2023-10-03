using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

List<string> adminUsernames = new List<string>
{
    "iztleu",
};
var token = "";
var botClient = new TelegramBotClient($"{token}");

using CancellationTokenSource cts = new ();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool. 
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

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
   if(update.Message is not {} message)
       return;
   
   if(message.Text is not {} text)
       return;
   
   var chatId = update.Message.Chat.Id;
   
   // ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
   // {
   //     KeyboardButton.WithRequestUser("Share User", RequestUser.Contact),
   // });

   // Message sentMessage = await botClient.SendTextMessageAsync(
   //     chatId: chatId,
   //     text: "Who or Where are you?",
   //     replyMarkup: replyKeyboardMarkup,
   //     cancellationToken: cancellationToken);
   
   // ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
   // {
   //     new KeyboardButton[] { "Help me" },
   //     new KeyboardButton[] { "Call me ☎️" },
   // })
   // {
   //     ResizeKeyboard = true
   //     
   // };
   //
   // Message sentMessage = await botClient.SendTextMessageAsync(
   //     chatId: chatId,
   //     text: "Choose a response",
   //     replyMarkup: replyKeyboardMarkup,
   //     cancellationToken: cancellationToken);
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

// Функция для проверки, является ли пользователь администратором
bool IsUserAdmin(string username)
{
    // Приведем username к нижнему регистру для сравнения без учета регистра
    username = username.ToLower();

    // Проверяем, содержится ли username в списке администраторов
    return adminUsernames.Contains(username);
}