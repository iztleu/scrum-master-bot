using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Action = Database.Models.Action;
using UserModel = Database.Models.User;

namespace TelegramBot;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ScrumMasterDbContext _dbContext;
    
    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, ScrumMasterDbContext dbContext)
    {
        _botClient = botClient;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;
        
        var user = await GetUser(message, cancellationToken);
        var userAction = await GetAction(user, cancellationToken);

        if (userAction is not null && messageText != Buttons.StopAction)
        {
            var actionResult = userAction.Type switch
            {
                ActionType.CreateScrumTeam => DoActionCreateScrumTeam(_botClient, userAction, user, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(ActionType))
            };
        }
        
        var action = messageText switch
        {
            // "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
            // "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
            // "/remove" => RemoveKeyboard(_botClient, message, cancellationToken),
            // "/photo" => SendFile(_botClient, message, cancellationToken),
            // "/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
            // "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
            // "/throw" => FailingHandler(_botClient, message, cancellationToken),
            Buttons.Start => Start(_botClient, message, cancellationToken),
            Buttons.CreateScrumTeam => CreateScrumTeam(_botClient, message, user, cancellationToken),
            Buttons.ShowMyScrumTeam => ShowMyScrumTeam(_botClient, message, user, cancellationToken),
            Buttons.StopAction => StopAction(_botClient, userAction, user, message, cancellationToken),
            _ => Start(_botClient, message, cancellationToken)
        };

        async Task<Message> ShowMyScrumTeam(ITelegramBotClient botClient, Message message, UserModel user, CancellationToken cancellationToken1)
        {
            var teams = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Members.Any(m => m.User.Id == user.Id))
                .ToListAsync(cancellationToken1);
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new (teams.Select(t => new KeyboardButton[] { t.Name })){
                ResizeKeyboard = true
            };;
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Выберите команду",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken1);
        }

        async Task<Message> StopAction(ITelegramBotClient botClient, Action action, UserModel user, Message message, CancellationToken cancellationToken)
        {
            _dbContext.Actions.Remove(action);
            _dbContext.SaveChanges();
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { Buttons.CreateScrumTeam},
                })
            {
                ResizeKeyboard = true
            };
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Действие отменено",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }  
        
        static async Task<Message> Start(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { Buttons.CreateScrumTeam, Buttons.ShowMyScrumTeam},
                })
            {
                ResizeKeyboard = true
            };
            try
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Здравствуйте, {message.From.Username}!",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        async Task<Message> CreateScrumTeam(ITelegramBotClient botClient, Message message, UserModel user, CancellationToken cancellationToken)
        {
            _dbContext.Actions.Add(new Action
            {
                Type = ActionType.CreateScrumTeam,
                UserId = user.Id
            });
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { Buttons.StopAction},
                })
            {
                ResizeKeyboard = true
            };
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Введите название команды",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> DoActionCreateScrumTeam(ITelegramBotClient botClient, Action action, UserModel user, Message message,
            CancellationToken cancellationToken)
        {
            var team = new ScrumTeam
            {
                Name = message.Text,
                CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                Members = new List<Member>
                {
                    new Member()
                    {
                        User = user,
                        Role = Role.Admin,
                    }
                }
            };
            
            await _dbContext.ScrumTeams.AddAsync(team);

            _dbContext.Actions.Remove(action);

            await _dbContext.SaveChangesAsync();
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Команда {team.Name} успешно создана",
                cancellationToken: cancellationToken);
        }
        
        // // Send inline keyboard
        // // You can process responses in BotOnCallbackQueryReceived handler
        // static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     await botClient.SendChatActionAsync(
        //         chatId: message.Chat.Id,
        //         chatAction: ChatAction.Typing,
        //         cancellationToken: cancellationToken);
        //
        //     // Simulate longer running task
        //     await Task.Delay(500, cancellationToken);
        //
        //     InlineKeyboardMarkup inlineKeyboard = new(
        //         new[]
        //         {
        //             // first row
        //             new []
        //             {
        //                 InlineKeyboardButton.WithCallbackData("1.1", "11"),
        //                 InlineKeyboardButton.WithCallbackData("1.2", "12"),
        //             },
        //             // second row
        //             new []
        //             {
        //                 InlineKeyboardButton.WithCallbackData("2.1", "21"),
        //                 InlineKeyboardButton.WithCallbackData("2.2", "22"),
        //             },
        //         });
        //
        //     return await botClient.SendTextMessageAsync(
        //         chatId: message.Chat.Id,
        //         text: "Choose",
        //         replyMarkup: inlineKeyboard,
        //         cancellationToken: cancellationToken);
        // }
        //
        // static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     ReplyKeyboardMarkup replyKeyboardMarkup = new(
        //         new[]
        //         {
        //                 new KeyboardButton[] { "1.1", "1.2" },
        //                 new KeyboardButton[] { "2.1", "2.2" },
        //         })
        //     {
        //         ResizeKeyboard = true
        //     };
        //
        //     return await botClient.SendTextMessageAsync(
        //         chatId: message.Chat.Id,
        //         text: "Choose",
        //         replyMarkup: replyKeyboardMarkup,
        //         cancellationToken: cancellationToken);
        // }
        //
        // static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     return await botClient.SendTextMessageAsync(
        //         chatId: message.Chat.Id,
        //         text: "Removing keyboard",
        //         replyMarkup: new ReplyKeyboardRemove(),
        //         cancellationToken: cancellationToken);
        // }
        //
        // static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     await botClient.SendChatActionAsync(
        //         message.Chat.Id,
        //         ChatAction.UploadPhoto,
        //         cancellationToken: cancellationToken);
        //
        //     const string filePath = "Files/tux.png";
        //     await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //     var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
        //
        //     return await botClient.SendPhotoAsync(
        //         chatId: message.Chat.Id,
        //         photo: new InputFileStream(fileStream, fileName),
        //         caption: "Nice Picture",
        //         cancellationToken: cancellationToken);
        // }
        //
        // static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     ReplyKeyboardMarkup RequestReplyKeyboard = new(
        //         new[]
        //         {
        //             KeyboardButton.WithRequestLocation("Location"),
        //             KeyboardButton.WithRequestContact("Contact"),
        //         });
        //
        //     return await botClient.SendTextMessageAsync(
        //         chatId: message.Chat.Id,
        //         text: "Who or Where are you?",
        //         replyMarkup: RequestReplyKeyboard,
        //         cancellationToken: cancellationToken);
        // }
        //
        //
        // static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     InlineKeyboardMarkup inlineKeyboard = new(
        //         InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));
        //
        //     return await botClient.SendTextMessageAsync(
        //         chatId: message.Chat.Id,
        //         text: "Press the button to start Inline Query",
        //         replyMarkup: inlineKeyboard,
        //         cancellationToken: cancellationToken);
        // }
        //
        // static Task<Message> FailingHandler(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     throw new IndexOutOfRangeException();
        // }
    }

  

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private async Task<UserModel> GetUser(Message message, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == message.From.Id, cancellationToken);
        if (user is null)
        {
            user = new()
            {
                TelegramUserId = message.From.Id,
                UserName = message.From.Username,
                ChatId = message.Chat.Id
            };
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
            
        if(user.ChatId != message.Chat.Id)
        {
            user.ChatId = message.Chat.Id;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return user;
    }
    
    private async Task<Action?> GetAction(UserModel user, CancellationToken cancellationToken)
    {
        return await _dbContext.Actions.FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);;
    }
}

public static class Buttons
{
    public const string Start = "/start";
    public const string StopAction = "Отменить действие";
    public const string CreateScrumTeam = "Создать Scrum команду";
    public const string ShowMyScrumTeam = "Показать мои Scrum команды";
    public const string JoinScrumTeam = "Присоединиться к Scrum команде";
    public const string LeaveScrumTeam = "Покинуть Scrum команду";
}