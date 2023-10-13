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

public class Start
{
    public record Request(long TelegramUserId, string TeamName, string VotingName) : IRequest<Response>;

    public record Response(long Id, string Title, string TeamName, string Status);
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

            RuleFor(x => x.VotingName)
                .NotEmpty()
                .WithErrorCode(NameRequired);
        }
    }
    
    public class Handler : IRequestHandler<Request, Response>
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

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var team = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Name == request.TeamName)
                .FirstOrDefaultAsync(cancellationToken);
            if (team == null)
            {
                throw new LogicConflictException("Team is note founded",TeamNotFound);
            }

            if(team.Members.All(m => m.User.TelegramUserId != request.TelegramUserId 
                                     && m.Role != Role.ScrumMaster))
            {
                throw new LogicConflictException("User is not a ScrumMaster of the team", UserIsNotScrumMaster);
            }
            
            if (await _dbContext.Votings
                    .AnyAsync(v => v.ScrumTeamId  == team.Id 
                                   && v.Title == request.VotingName
                        && v.Status != VotingStatus.Finished, cancellationToken))
            {
                throw new LogicConflictException("Voting with the same name already exists", VotingAlreadyExists);
            } 
            
            if (await _dbContext.Votings
                    .AnyAsync(v => v.ScrumTeamId  == team.Id 
                                   && v.Status != VotingStatus.Finished, cancellationToken))
            {
                throw new LogicConflictException("Active Voting already exists", VotingAlreadyExists);
            } 
            
            var voting = new Domain.Models.Voting
            {
                Title = request.VotingName,
                CreatedAt = DateTime.UtcNow,
                ScrumTeam = team,
                Status = VotingStatus.Created
            };
            
            await _dbContext.Votings.AddAsync(voting, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            await _publisher.Publish(new VotingStartEvent(voting.Id), cancellationToken);
            
            return new Response(voting.Id, voting.Title, voting.ScrumTeam.Name, voting.Status.ToString());
        }
    }
}

