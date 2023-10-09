// using Database;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Telegram.Bot;
// using Telegram.Bot.Types;
// using UserModel = Database.Models.User;
// using Action = Database.Models.Action;
// public class UpdateHandlerTwo
// {
//     private readonly ITelegramBotClient _botClient;
//     private readonly ILogger<UpdateHandlerTwo> _logger;
//     private readonly ScrumMasterDbContext _dbContext;
//     private readonly IUserActionHandlerFactory _userActionHandlerFactory;
//     private readonly ICommandHandlerFactory _commandHandlerFactory;
//
//     public UpdateHandlerTwo(ITelegramBotClient botClient, ILogger<UpdateHandlerTwo> logger, ScrumMasterDbContext dbContext, IUserActionHandlerFactory userActionHandlerFactory, ICommandHandlerFactory commandHandlerFactory)
//     {
//         _botClient = botClient;
//         _logger = logger;
//         _dbContext = dbContext;
//         _userActionHandlerFactory = userActionHandlerFactory;
//         _commandHandlerFactory = commandHandlerFactory;
//     }
//
//     public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
//     {
//         if (update.Type == UpdateType.Unknown)
//         {
//             _logger.LogWarning("Received unknown update type");
//             return;
//         }
//
//         if (update.Message is not null)
//         {
//             await HandleMessageAsync(update.Message, cancellationToken);
//         }
//         else if (update.CallbackQuery is not null)
//         {
//             await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
//         }
//     }
//
//     private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
//     {
//         var user = await GetUserAsync(message.From.Id, cancellationToken);
//         var messageText = message.Text;
//
//         var action = messageText switch
//         {
//             Buttons.Start => new StartAction(_botClient, _logger, _dbContext, user),
//             Buttons.CreateScrumTeam => new CreateScrumTeamAction(_botClient, _logger, _dbContext, user),
//             Buttons.ShowMyScrumTeam => new ShowMyScrumTeamAction(_botClient, _logger, _dbContext, user),
//             Buttons.StopAction => new StopAction(_botClient, _logger, _dbContext, user),
//             _ => new StartAction(_botClient, _logger, _dbContext, user)
//         };
//
//         await action.ExecuteAsync(message, cancellationToken);
//     }
//
//     private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
//     {
//         var user = await GetUserAsync(callbackQuery.From.Id, cancellationToken);
//         var userAction = await GetUserActionAsync(user, cancellationToken);
//
//         if (userAction is not null && callbackQuery.Data != Buttons.StopAction)
//         {
//             var action = _userActionHandlerFactory.Create(userAction.Type);
//             await action.ExecuteAsync(callbackQuery, userAction, user, cancellationToken);
//             return;
//         }
//
//         var command = _commandHandlerFactory.Create(callbackQuery.Data);
//         await command.ExecuteAsync(callbackQuery, user, cancellationToken);
//     }
//
//     private async Task<UserModel> GetUserAsync(long userId, CancellationToken cancellationToken)
//     {
//         var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
//         if (user is null)
//         {
//             user = new UserModel { TelegramUserId = userId };
//             _dbContext.Users.Add(user);
//             await _dbContext.SaveChangesAsync(cancellationToken);
//         }
//
//         return user;
//     }
//
//     private async Task<Action?> GetUserActionAsync(UserModel user, CancellationToken cancellationToken)
//     {
//         return await _dbContext.Actions.FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);;
//     }
// }
//
// public static class Buttons
// {
//     public const string Start = "/start";
//     public const string StopAction = "Отменить действие";
//     public const string CreateScrumTeam = "Создать Scrum команду";
//     public const string ShowMyScrumTeam = "Показать мои Scrum команды";
//     public const string JoinScrumTeam = "Присоединиться к Scrum команде";
//     public const string LeaveScrumTeam = "Покинуть Scrum команду";
// }
