using App.Services;
using Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace App.Features.Voting.Events;

public class VotingStartEventHandler: INotificationHandler<VotingStartEvent>
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ScrumMasterDbContext _dbContext;
    private readonly ILogger<VotingStartEventHandler> _logger;

    public VotingStartEventHandler(
        ScrumMasterDbContext dbContext, 
        ITelegramBotClient telegramBotClient, 
        ILogger<VotingStartEventHandler> logger)
    {
        _dbContext = dbContext;
        _telegramBotClient = telegramBotClient;
        _logger = logger;
    }

    public async Task Handle(VotingStartEvent notification, CancellationToken cancellationToken)
    {
        var voting = await _dbContext.Votings
            .AsNoTracking()
            .Include(v => v.Votes)
            .ThenInclude(v => v.Member)
            .Include(v => v.ScrumTeam)
            .ThenInclude(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(v => v.Id == notification.VotingId, cancellationToken: cancellationToken);
        
        if (voting == null)
        {
            _logger.LogError($"Voting with id {notification.VotingId} was not found");
            return;
        }
        var members = voting.ScrumTeam.Members;
        
        for( var i = 0; i < members.Count; i++)
        {
            if (voting.Votes.Any(v => v.Member.Id == members[i].Id))
            {
                _logger.LogInformation($"Voting {voting.Title} was already started for member {members[i].User.TelegramUserId}");
                continue;
            }
            
            var message = $"Voting {voting.Title} was started";
            await _telegramBotClient
                .SendTextMessageAsync(
                    members[i].User.TelegramUserId,
                    message, 
                    replyMarkup: new InlineKeyboardMarkup(GetInlineKeyboardButtons(voting)),
                    cancellationToken: cancellationToken);
        }
    }
    
    public static IEnumerable<InlineKeyboardButton> GetInlineKeyboardButtons(Domain.Models.Voting voting)
    {
        return new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData("1", $"{CallbackQueryData.VoteRequest};{voting.Id};1"),
            InlineKeyboardButton.WithCallbackData("2", $"{CallbackQueryData.VoteRequest};{voting.Id};2"),
            InlineKeyboardButton.WithCallbackData("3", $"{CallbackQueryData.VoteRequest};{voting.Id};3"),
            InlineKeyboardButton.WithCallbackData("5", $"{CallbackQueryData.VoteRequest};{voting.Id};5"),
            InlineKeyboardButton.WithCallbackData("8", $"{CallbackQueryData.VoteRequest};{voting.Id};8"),
            InlineKeyboardButton.WithCallbackData("13", $"{CallbackQueryData.VoteRequest};{voting.Id};13"),
            InlineKeyboardButton.WithCallbackData("pass", $"{CallbackQueryData.VoteRequest};{voting.Id};pass"),
        };
    }
}