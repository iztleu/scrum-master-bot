using Database;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Action = Domain.Models.Action;
using UserModel = Domain.Models.User;

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
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
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
                ActionType.ShowTeam => DoActionShowTeam(_botClient, userAction, user, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(ActionType))
            };
            
            return;
        }
        
        var action = messageText switch
        {            
            Buttons.Start => Start(_botClient, message, user, cancellationToken),
            Buttons.CreateScrumTeam => CreateScrumTeam(_botClient, message, user, cancellationToken),
            Buttons.ShowMyScrumTeam => ShowMyScrumTeam(_botClient, message, user, cancellationToken),
            Buttons.StopAction => StopAction(_botClient, userAction, user, message, cancellationToken),
            _ => Start(_botClient, message, user, cancellationToken)
        };

        async Task<Message> ShowMyScrumTeam(ITelegramBotClient botClient, Message message, UserModel user, CancellationToken cancellationToke)
        {
            var teams = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Members.Any(m => m.User.Id == user.Id))
                .ToListAsync(cancellationToken);

            ReplyKeyboardMarkup replyKeyboardMarkup;
            if (teams.Count == 0)
            {
                replyKeyboardMarkup = new(
                    new[]
                    {
                        new KeyboardButton[] { Buttons.CreateScrumTeam, Buttons.ShowMyScrumTeam},
                    })
                {
                    ResizeKeyboard = true
                };

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    replyMarkup: replyKeyboardMarkup,
                    text: $"У вас нету команд",
                    cancellationToken: cancellationToken);
            }
            
            replyKeyboardMarkup = new (teams.Select(t => new KeyboardButton[] { t.Name })){
                ResizeKeyboard = true
            };;
            
            await _dbContext.Actions.AddAsync(new Action
            {
                Type = ActionType.ShowTeam,
                UserId = user.Id
            });
            
            await _dbContext.SaveChangesAsync(cancellationToke);
            
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Выберите команду",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        async Task<Message> StopAction(ITelegramBotClient botClient, Action action, UserModel user, Message message, CancellationToken cancellationToken)
        {
            _dbContext.Actions.Remove(action);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
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
        
        async Task<Message> Start(ITelegramBotClient botClient, Message message, UserModel user, CancellationToken cancellationToken)
        {
            var teams = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Members.Any(m => m.User.Id == user.Id))
                .ToListAsync(cancellationToken);

            var keyButtons = new List<KeyboardButton> { Buttons.CreateScrumTeam };
            if (teams.Count != 0)
            {
                keyButtons.Add(new KeyboardButton(Buttons.ShowMyScrumTeam));
            }
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    keyButtons
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
            await _dbContext.Actions.AddAsync(new Action
            {
                Type = ActionType.CreateScrumTeam,
                UserId = user.Id
            }, cancellationToken:cancellationToken);
            
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
                    new ()
                    {
                        User = user,
                        Role = Role.ScrumMaster,
                    }
                }
            };
            
            await _dbContext.ScrumTeams.AddAsync(team, cancellationToken);

            _dbContext.Actions.Remove(action);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Команда {team.Name} успешно создана",
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> DoActionShowTeam(ITelegramBotClient botClient, Action action, UserModel user, Message message,
            CancellationToken cancellationToken)
        {
            var team = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Name == message.Text)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (team is null)
            {
                _dbContext.Actions.Remove(action);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Команда не найдена",
                    cancellationToken: cancellationToken);
            }
            
            List<List<InlineKeyboardButton>> inlineKeyboardButtons = new();
            var row = 0;
            foreach (var member in team.Members)
            {
                if (inlineKeyboardButtons.Count == row)
                {
                    inlineKeyboardButtons.Add(new List<InlineKeyboardButton>());
                }
                if (inlineKeyboardButtons[row].Count == 2)
                {
                    row++;
                    inlineKeyboardButtons.Add(new List<InlineKeyboardButton>());
                }

                var name = member.Role == Role.ScrumMaster ? $"{member.User.UserName} {member.Role}" : member.User.UserName;
                inlineKeyboardButtons[row].Add(InlineKeyboardButton.WithUrl(name, $"https://t.me/{member.User.UserName}"));
            }
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { Buttons.StopAction},
                })
            {
                ResizeKeyboard = true
            };
            
            _dbContext.Actions.Remove(action);

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Команда {team.Name}",
                replyMarkup: new InlineKeyboardMarkup(inlineKeyboardButtons),
                cancellationToken: cancellationToken);
        }
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