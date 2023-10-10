using App.Features.ScrumTeam.Requests;
using Database;
using Domain.Models;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Action = Domain.Models.Action;
using UserModel = Domain.Models.User;

namespace App.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ActionService _actionService;
    
    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger,
        IMediator mediator, ActionService actionService)
    {
        _botClient = botClient;
        _logger = logger;
        _mediator = mediator;
        _actionService = actionService;
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

        var userAction = message.From != null? await GetAction(message.From.Id, cancellationToken):null;

        if (userAction is not null && messageText != Buttons.StopAction)
        {
            var actionResult = userAction.Type switch
            {
                ActionType.CreateScrumTeam => DoActionCreateScrumTeam(_botClient, userAction, message, cancellationToken),
                ActionType.ShowTeam => DoActionShowTeam(_botClient, userAction, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(ActionType))
            };
            
            return;
        }
        
        var action = messageText switch
        {            
            Buttons.Start => Start(_botClient, message, cancellationToken),
            Buttons.CreateScrumTeam => CreateScrumTeam(_botClient, message, cancellationToken),
            Buttons.ShowMyScrumTeam => ShowMyScrumTeam(_botClient, message, cancellationToken),
            Buttons.StopAction => StopAction(_botClient, userAction, message, cancellationToken),
            _ => Start(_botClient, message, cancellationToken)
        };
        
        async Task<Message> Start(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new GetAllMyScrumTeam.Request(message.GetTelegramId()), cancellationToken);

            var keyButtons = new List<KeyboardButton> { Buttons.CreateScrumTeam };
            if (response.teams.Length != 0)
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
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Здравствуйте, {message.From.Username}!",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        async Task<Message> CreateScrumTeam(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await _actionService.CreateActionAsync(new()
            {
                TelegramUserId = message.GetTelegramId(),
                Type = ActionType.CreateScrumTeam
            });
            
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
        
        async Task<Message> ShowMyScrumTeam(ITelegramBotClient botClient, Message message, CancellationToken cancellationToke)
        {
            var response = await _mediator.Send(new GetAllMyScrumTeam.Request(message.GetTelegramId()), cancellationToken);

            ReplyKeyboardMarkup replyKeyboardMarkup;
            if (response.teams.Length == 0)
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

            var teamButtons = response.teams.Select(t => new KeyboardButton[] { t.Name }).ToList();
            
            teamButtons.Add(new KeyboardButton[] { Buttons.StopAction});
            
            replyKeyboardMarkup = new (teamButtons){
                ResizeKeyboard = true
            };
            
            await _actionService.CreateActionAsync(new()
            {
                TelegramUserId = message.GetTelegramId(),
                Type = ActionType.ShowTeam
            });
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Выберите команду",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> StopAction(ITelegramBotClient botClient, Action? action, Message message, CancellationToken cancellationToken)
        {
            if (action is not null)
                await _actionService.DeleteActionAsync(action);
            
            var response = await _mediator.Send(new GetAllMyScrumTeam.Request(message.GetTelegramId()), cancellationToken);

            var keyButtons = new List<KeyboardButton> { Buttons.CreateScrumTeam };
            if (response.teams.Length != 0)
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
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Действие отменено",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }  
        
        async Task<Message> DoActionCreateScrumTeam(ITelegramBotClient botClient, Action action, Message message,
            CancellationToken cancellationToken)
        {
            var createResponse = await _mediator.Send(new CreateScrumTeam.Request(message.GetTelegramId(), message.Text), cancellationToken);
            
            await _actionService.DeleteActionAsync(action);
            
            var response = await _mediator.Send(new GetAllMyScrumTeam.Request(message.GetTelegramId()), cancellationToken);

            var keyButtons = new List<KeyboardButton> { Buttons.CreateScrumTeam };
            if (response.teams.Length != 0)
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
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Команда {createResponse.Name} успешно создана",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> DoActionShowTeam(ITelegramBotClient botClient, Action action, Message message,
            CancellationToken cancellationToken)
        {
            var team = (await _mediator.Send(new GetScrumTeamByName.Request(message.GetTelegramId(), message.Text), cancellationToken)).Team;

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
    
    private async Task<Action?> GetAction(long telegramUserId, CancellationToken cancellationToken)
    {
        return (await _actionService.GetActionsAsync(telegramUserId, cancellationToken)).FirstOrDefault();
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

public static class Extensions
{
    public static long GetTelegramId(this Message message)
    {
        return message.From?.Id??0;
    }
}