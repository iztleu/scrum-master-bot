using System.Text.Json.Serialization;
using App.Errors.Exceptions;
using App.Features.Members.Requests;
using App.Features.ScrumTeam.Requests;
using App.Features.User.Requests;
using App.Features.Voting.Requests;
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
    
    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        
    }
    
    private async Task HandleMessageExceprion(Message message, Exception exception)
    {
        var text = HandleException(exception);

        await _actionService.DeleteActionsAsync(message.GetTelegramId());
        
        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId());
        
        await _botClient.SendTextMessageAsync(
            chatId: message.GetTelegramId(),
            replyMarkup: replyKeyboardMarkup,
            text: text);
    }

    private async Task HandleCallbackException(CallbackQuery callbackQuery, Exception exception)
    {
        var text = HandleException(exception);

        await _actionService.DeleteActionsAsync(callbackQuery.From.Id);


        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id);
        var replyKeyboardMarkup = await CreateCommonButtonAsync(callbackQuery.From.Id);
        
        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.From.Id,
            replyMarkup: replyKeyboardMarkup,
            text: text);
    }

    private string HandleException(Exception exception)
    {
        return exception switch
        {
            ValidationErrorsException ex => "Validation errors " +
                                            ex.Errors.Select(x => $"{x.Field}: {x.Code}")
                                                .Aggregate((x, y) => $"{x}\n{y}"),
            LogicConflictException ex => $"Logic conflict {ex.Code}",
            OperationCanceledException => "Request timed out \ud83d\udd54",
            InternalErrorException => "Something went wrong \ud83e\udd37\u200d\u2642\ufe0f",
            _ => "Something went wrong \ud83e\udd37\u200d\u2642\ufe0f"
        };
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Receive message type: {MessageType}", message.Type);
            if (message.Text is not { } messageText)
                return;

            var userAction = message.From is not null ? await GetAction(message.From.Id, cancellationToken) : null;

            if (userAction is not null && messageText != Buttons.StopAction)
            {
                var actionResult =  userAction.Type switch
                {
                    ActionType.CreateScrumTeam => await DoActionCreateScrumTeam(_botClient, userAction, message, cancellationToken),
                    ActionType.ShowAllTeams => await DoActionShowAllTeam(_botClient, userAction, message, cancellationToken),
                    ActionType.JoinToScrumTeam => await DoActionJoinToScrumTeam(_botClient, userAction, message, cancellationToken),
                    ActionType.ChooseScrumTeamActions => await DoActionChooseScrumTeamActions(_botClient, userAction, message, cancellationToken),
                    ActionType.RenameScrumTeam => await DoActionRenameScrumTeam(_botClient, userAction, message, cancellationToken),
                    ActionType.StartVoting => await DoActionStartVoting(_botClient, userAction, message, cancellationToken),
                    _ => await UnknownAction(_botClient, userAction, message, cancellationToken)
                };
            
                _logger.LogInformation("The message was sent with id: {SentMessageId}", actionResult.MessageId);
                return;
            }

            var action = messageText switch
            {
                Buttons.Start => Start(_botClient, message, cancellationToken),
                Buttons.CreateScrumTeam => CreateScrumTeam(_botClient, message, cancellationToken),
                Buttons.ShowMyScrumTeams => ShowMyScrumTeams(_botClient, message, cancellationToken),
                Buttons.JoinScrumTeam => JoinToScrumTeam(_botClient, message, cancellationToken),
                Buttons.StopAction => StopAction(_botClient, userAction, message, cancellationToken),
                _ => Start(_botClient, message, cancellationToken)
            };
        
            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing message");
            await HandleMessageExceprion(message, e);
        }
    }

    private async Task<Message> DoActionStartVoting(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        var teamName = userAction.AdditionInfo!.Split("=").Last();
        var votingName = message.Text;
        await _mediator.Send(new Start.Request(message.GetTelegramId(), teamName, votingName), cancellationToken);
        await _actionService.DeleteActionAsync(userAction);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Голосование {votingName} началось",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DoActionRenameScrumTeam(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        var oldTeamName = userAction.AdditionInfo!.Split("=").Last();
        await _mediator.Send(new RenameScrumTeam.Request(
                message.GetTelegramId(), 
                oldTeamName,
                message.Text),
            cancellationToken);
        
        await _actionService.DeleteActionAsync(userAction);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Команда {oldTeamName} успешно переименована в {message.Text}",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> UnknownAction(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<Message> DoActionChooseScrumTeamActions(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        var action = message.Text switch
        {
            Buttons.RenameScrumTeam => await DoActionChooseRenameScrumTeam(_botClient, userAction, message, cancellationToken),
            Buttons.ShowMembers => await DoActionChooseShowMembers(_botClient, userAction, message, cancellationToken),
            Buttons.LeaveScrumTeam => await DoActionChooseLeaveScrumTeam(_botClient, userAction, message, cancellationToken),
            Buttons.StartVoting => await DoActionChooseStartVoting(_botClient, userAction, message, cancellationToken),
            _ => await UnknownAction(_botClient, userAction, message, cancellationToken)
        };

        return action;
    }

    private async Task<Message> DoActionChooseStartVoting(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    { 
        userAction.Type = ActionType.StartVoting;
        await _actionService.UpdateActionAsync(userAction);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Введите название задачи",
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DoActionChooseLeaveScrumTeam(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        var teamName = userAction.AdditionInfo!.Split("=").Last();
        await _mediator.Send(new LeaveTeam.Request(message.GetTelegramId(), teamName), cancellationToken);
        
        await _actionService.DeleteActionAsync(userAction);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Вы покинули команду {teamName}",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DoActionChooseRenameScrumTeam(ITelegramBotClient botClient, Action userAction, Message message, CancellationToken cancellationToken)
    {
        userAction.Type = ActionType.RenameScrumTeam;
        await _actionService.UpdateActionAsync(userAction);
        
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Введите новое название команды {userAction.AdditionInfo.Split('=').Last()}",
            cancellationToken: cancellationToken);
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

            if (callbackQuery.Data is not null)
            {
                var memberId = callbackQuery.Data.Split(" ").Last();
                if (callbackQuery.Data.Contains(CallbackQueryData.AcceptInviteRequest))
                {
                    await _mediator.Send(new AcceptInvite.Request(callbackQuery.From.Id, int.Parse(memberId)),
                        cancellationToken);
                }
                if (callbackQuery.Data.Contains(CallbackQueryData.DeclineInviteRequest))
                {
                    await _mediator.Send(new DeclineInvite.Request(callbackQuery.From.Id, int.Parse(memberId)),
                        cancellationToken);
                }

                await _botClient.AnswerCallbackQueryAsync(callbackQueryId: callbackQuery.Id, cancellationToken: cancellationToken);
            
                return;
            }
        
            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}",
                cancellationToken: cancellationToken);

            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: $"Received {callbackQuery.Data}",
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing callback");
            await HandleCallbackException(callbackQuery, e);
        }
    }


    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

   
    private async Task<Action?> GetAction(long telegramUserId, CancellationToken cancellationToken)
    {
        return (await _actionService.GetActionsAsync(telegramUserId, cancellationToken)).FirstOrDefault();
    }

    private async Task<Message> Start(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var user = (await _mediator.Send(new GetUser.Request(message.GetTelegramId()), cancellationToken)).User;
        if (user is null)
        {
            await _mediator.Send(
                new ReqistrateUser.Request(
                    new(message.From.Username, message.GetTelegramId())
                    )
                );
        }
        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Здравствуйте, {message.From.Username}!",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<ReplyKeyboardMarkup> CreateCommonButtonAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetAllMyScrumTeam.Request(telegramId), cancellationToken);

        var keyButtons = new List<KeyboardButton> { Buttons.CreateScrumTeam, Buttons.JoinScrumTeam };
        if (response.teams.Length != 0)
        {
            keyButtons.Add(new KeyboardButton(Buttons.ShowMyScrumTeams));
        }

        ReplyKeyboardMarkup replyKeyboardMarkup = new(
            new[]
            {
                keyButtons
            })
        {
            ResizeKeyboard = true
        };
        return replyKeyboardMarkup;
    }

    private async Task<Message> CreateScrumTeam(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await _actionService.CreateActionAsync(new()
        {
            TelegramUserId = message.GetTelegramId(),
            Type = ActionType.CreateScrumTeam
        });

        ReplyKeyboardMarkup replyKeyboardMarkup = new(
            new[]
            {
                new KeyboardButton[] { Buttons.StopAction },
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

    private async Task<Message> ShowMyScrumTeams(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetAllMyScrumTeam.Request(message.GetTelegramId()), cancellationToken);

        ReplyKeyboardMarkup replyKeyboardMarkup;
        if (response.teams.Length == 0)
        {
            replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                replyMarkup: replyKeyboardMarkup,
                text: $"У вас нету команд",
                cancellationToken: cancellationToken);
        }

        var teamButtons = response.teams.Select(t => new KeyboardButton[] { t.Name }).ToList();

        teamButtons.Add(new KeyboardButton[] { Buttons.StopAction });

        replyKeyboardMarkup = new(teamButtons)
        {
            ResizeKeyboard = true
        };

        await _actionService.CreateActionAsync(new()
        {
            TelegramUserId = message.GetTelegramId(),
            Type = ActionType.ShowAllTeams
        });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Выберите команду",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> JoinToScrumTeam(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup replyKeyboardMarkup = new(
            new[]
            {
                new KeyboardButton[] { Buttons.StopAction },
            })
        {
            ResizeKeyboard = true
        };
        
        await _actionService.CreateActionAsync(new()
        {
            TelegramUserId = message.GetTelegramId(),
            Type = ActionType.JoinToScrumTeam
        });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Введите название команды",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> StopAction(ITelegramBotClient botClient, Action? action, Message message,
        CancellationToken cancellationToken)
    {
        if (action is not null)
            await _actionService.DeleteActionAsync(action);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Действие отменено",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DoActionCreateScrumTeam(ITelegramBotClient botClient, Action action, Message message,
        CancellationToken cancellationToken)
    {
        var createResponse = await _mediator.Send(new CreateScrumTeam.Request(message.GetTelegramId(), message.Text),
            cancellationToken);
        await _actionService.DeleteActionAsync(action);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Команда {createResponse.Name} успешно создана",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DoActionShowAllTeam(ITelegramBotClient botClient, Action action, Message message,
        CancellationToken cancellationToken)
    {
        var keyButtonsL1 = new List<KeyboardButton> { Buttons.RenameScrumTeam,  Buttons.LeaveScrumTeam};
        var keyButtonsL2 = new List<KeyboardButton> { Buttons.StartVoting, Buttons.ShowMembers };
        var keyButtonsL3 = new List<KeyboardButton> { Buttons.StopAction };
        
        ReplyKeyboardMarkup replyKeyboardMarkup = new (new[]
        {
            keyButtonsL1,
            keyButtonsL2,
            keyButtonsL3,
        })
        {
            ResizeKeyboard = true
        };

        await _actionService.DeleteActionAsync(action);
        await _actionService.CreateActionAsync(new Action()
        {
            TelegramUserId = message.GetTelegramId(),
            Type = ActionType.ChooseScrumTeamActions,
            AdditionInfo = $"tema_name={message.Text}"
        });
        
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Выберите действие",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
    
    private async Task<Message> DoActionChooseShowMembers(ITelegramBotClient botClient, Action action, Message message,
        CancellationToken cancellationToken)
    {
        var team = (await _mediator.Send(
            new GetScrumTeamByName.Request(message.GetTelegramId(), 
                action.AdditionInfo.Split("=")
                    .Last()),
            cancellationToken)).Team;

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
    
    private async Task<Message> DoActionJoinToScrumTeam(ITelegramBotClient botClient, Action userAction, Message message, 
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SendInviteRequest.Request(message.GetTelegramId(), message.Text),
            cancellationToken);

        await _actionService.DeleteActionAsync(userAction);

        var replyKeyboardMarkup = await CreateCommonButtonAsync(message.GetTelegramId(), cancellationToken);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Запрос на присоединение к команде {message.Text} отправлен",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
}

public static class Buttons
{
    public const string Start = "/start";
    public const string StopAction = "Отменить действие";
    public const string CreateScrumTeam = "Создать Scrum команду";
    public const string ShowMyScrumTeams = "Показать мои Scrum команды";
    public const string JoinScrumTeam = "Присоединиться к Scrum команде";
    public const string RenameScrumTeam = "Переименовать Scrum команду";
    public const string ShowMembers = "Показать участников Scrum команды";
    public const string LeaveScrumTeam = "Покинуть Scrum команду";
    public const string StartVoting = "Начать голосование";
}

public static class CallbackQueryData
{
    public const string AcceptInviteRequest = "accept invite request";
    public const string DeclineInviteRequest = "decline invite request";
    public const string VoteRequest = "vote request";
}

public static class Extensions
{
    public static long GetTelegramId(this Message message)
    {
        return message.From?.Id ?? 0;
    }
}

internal record ErrorData(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string Code);