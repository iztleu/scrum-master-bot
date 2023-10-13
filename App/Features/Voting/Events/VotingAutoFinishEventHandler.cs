using Database;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace App.Features.Voting.Events;

public class VotingAutoFinishEventHandler : INotificationHandler<VotingAutoFinishEvent>
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ScrumMasterDbContext _dbContext;
    private readonly ILogger<VotingAutoFinishEventHandler> _logger;

    public VotingAutoFinishEventHandler(
        ScrumMasterDbContext dbContext, 
        ITelegramBotClient telegramBotClient, 
        ILogger<VotingAutoFinishEventHandler> logger)
    {
        _dbContext = dbContext;
        _telegramBotClient = telegramBotClient;
        _logger = logger;
    }

    public async Task Handle(VotingAutoFinishEvent notification, CancellationToken cancellationToken)
    {
        var voting = await _dbContext.Votings
            .Include(v => v.Votes)
            .ThenInclude(v => v.Member)
            .ThenInclude(m => m.User)
            .Include(v => v.ScrumTeam)
            .ThenInclude(scrumTeam => scrumTeam.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(v => v.Id == notification.VotingId, cancellationToken: cancellationToken);

        var score = new List<int>();
        var pass = new List<Vote>();
        for(var i = 0; i < voting.Votes.Count; i++)
        {
            if (int.TryParse(voting.Votes[i].Value, out var vote))
            {
                score.Add(vote);
            }
            else
            {
                pass.Add(voting.Votes[i]);
            }
        }
            
        var message = $"Voting {voting.Title} was finished. Average: {score.Average()}, Max: {score.Max()}, Min: {score.Min()}";
        var passMessage = pass.Select(m => $"\n{m.Member.User.UserName} voted {m.Value}");
        
        voting.Status = VotingStatus.Finished;
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        await _telegramBotClient
            .SendTextMessageAsync(
                voting.ScrumTeam.Members.First(m => m.Role == Role.ScrumMaster).User.TelegramUserId,
                message + string.Join("", passMessage), 
                cancellationToken: cancellationToken);
    }
}