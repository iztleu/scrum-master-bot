using App.Errors.Exceptions;
using App.Features.Voting.Events;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using static App.Features.Voting.Errors.VotingValidationErrors;

namespace App.Features.Voting.Requests;

public class FinishVoting
{
    public record Request(long TelegramUserId, long VotingId) : IRequest;
    
    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
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
                .AsNoTracking()
                .Include(v => v.ScrumTeam)
                .ThenInclude(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(cancellationToken);
            if (voting == null)
            {
                throw new LogicConflictException("Team is note founded",TeamNotFound);
            }

            if(voting.ScrumTeam.Members.All(m => m.User.TelegramUserId != request.TelegramUserId 
                                     && m.Role != Role.ScrumMaster))
            {
                throw new LogicConflictException("User is not a ScrumMaster of the team", UserIsNotScrumMaster);
            }
            
            await _publisher.Publish(new VotingFinishEvent(request.VotingId), cancellationToken);
        }
    }
}

