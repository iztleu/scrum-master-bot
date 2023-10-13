using App.Errors.Exceptions;
using App.Features.Voting.Events;
using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
namespace App.Features.Voting.Requests;
using static App.Features.Voting.Errors.VotingValidationErrors;

public class Vote
{
    public record Request(long TelegramUserId, long VotingId, string value) : IRequest;
    
    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext.Users
                        .AnyAsync(u => u.TelegramUserId == id, cancellationToken);
                    return teamExist;
                }).WithErrorCode(UserNotFound);
            
            RuleFor(x => x.VotingId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var votingExist = await dbContext.Votings
                        .AnyAsync(v => v.Id == id, cancellationToken);
                    return votingExist;
                }).WithErrorCode(VotingNotFound);
        }
    }

    public class Handler : IRequestHandler<Request>
    {
        private readonly ScrumMasterDbContext _dbContext;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly IPublisher _publisher;
        public Handler(ScrumMasterDbContext dbContext, ITelegramBotClient telegramBotClient, IPublisher publisher)
        {
            _dbContext = dbContext;
            _telegramBotClient = telegramBotClient;
            _publisher = publisher;
        }

        public async Task Handle(Request request, CancellationToken cancellationToken)
        {
            var voting = await _dbContext.Votings
                .Include(v => v.Votes)
                .ThenInclude(vote => vote.Member)
                .Include(v => v.ScrumTeam)
                .ThenInclude(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(v => v.Id == request.VotingId, cancellationToken: cancellationToken);

            var member = voting.ScrumTeam.Members.FirstOrDefault(m => m.User.TelegramUserId == request.TelegramUserId);
            if (member == null)
            {
                throw new LogicConflictException("User is not a member of the team", UserIsNotMember);
            }

            if (voting.Votes.Any(v => v.Member.Id == member.Id))
            {
                throw new LogicConflictException("User has already voted", UserAlreadyVoted);
            }

            voting.Votes.Add(new Domain.Models.Vote()
            {
                Member = member,
                Value = request.value,
                CreatedAt = DateTime.UtcNow,
            });
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            if (voting.Votes.Count == voting.ScrumTeam.Members.Count)
            {
                await _publisher.Publish(new VotingAutoFinishEvent(voting.Id), cancellationToken);
            }
        }
    }
}