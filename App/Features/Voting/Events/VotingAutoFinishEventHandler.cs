using System.Dynamic;
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
        for(var i = 0; i < voting!.Votes.Count; i++)
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
        var min = score.Min();
        var max = score.Max();
            
        var message = $"Voting {voting.Title} was finished. Average: {score.Average()}, Max: {max}, Min: {min}";
        var passMessage = pass.Select(m => $"\n{m.Member.User!.UserName} voted {m.Value}");
        if(!CheckRules(min, max))
        {
            message += "\nWarning! There is a big difference between the minimum and maximum values";
        }
        voting.Status = VotingStatus.Finished;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tasks = voting.ScrumTeam.Members.Select(async m =>
        {
            await _telegramBotClient
                .SendTextMessageAsync(
                    m.User!.TelegramUserId,
                    message + string.Join("", passMessage),
                    cancellationToken: cancellationToken);
        });
        
        await Task.WhenAll(tasks);
    }

    private static bool CheckRules(int min, int max)
    {
        var minIndex = GetIndex(min);
        var maxIndex = GetIndex(max);

        return maxIndex - minIndex <= 3;
    }

    private static int GetIndex(int value)
    {
        return value switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            5 => 3,
            8 => 4,
            13 => 5,
            _ => 6
        };
    }
}