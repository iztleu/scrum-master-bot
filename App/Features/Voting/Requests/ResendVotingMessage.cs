using App.Features.Voting.Events;
using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static App.Features.Voting.Errors.VotingValidationErrors;

namespace App.Features.Voting.Requests;

class ResendVotingMessage
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
        private readonly IPublisher _publisher;

        public Handler(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Handle(Request request, CancellationToken cancellationToken)
        {
            await _publisher.Publish(new VotingStartEvent(request.VotingId), cancellationToken);
        }
    }
}