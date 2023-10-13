using App.Errors.Exceptions;
using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static App.Features.Voting.Errors.VotingValidationErrors;

namespace App.Features.Voting.Requests;

public class GetStartedVoting
{
    public record Request(long TelegramUserId, string TeamName) : IRequest<Response>;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var userExist = await dbContext.Users
                        .AnyAsync(u => u.TelegramUserId == id, cancellationToken);
                    return userExist;
                }).WithErrorCode(UserNotFound);

            RuleFor(x => x.TeamName)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams
                        .AsNoTracking()
                        .AnyAsync(t => t.Name == name,
                            cancellationToken);
                    return teamExist;
                }).WithErrorCode(TeamNotFound);
        }
    }
    
    public class Response
    {
        public Response(long id, string title, string teamName, string status)
        {
            Id = id;
            Title = title;
            TeamName = teamName;
            Status = status;
        }

        public long Id { get; }
        public string Title { get; }
        public string TeamName { get; }
        public string Status { get; }
    }
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var voting = await _dbContext.Votings
                .AsNoTracking()
                .Include(v => v.ScrumTeam)
                .Where(v => v.ScrumTeam.Name == request.TeamName 
                            && v.Status != Domain.Models.VotingStatus.Finished)
                .FirstOrDefaultAsync(cancellationToken);
            if (voting == null)
            {
                throw new LogicConflictException("Voting is not founded",VotingNotFound);
            }

            return new Response(voting.Id, voting.Title, voting.ScrumTeam.Name, voting.Status.ToString());
        }
    }
}