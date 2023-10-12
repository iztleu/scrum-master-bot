using App.Errors.Exceptions;
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
    public record Request(long TelegramUserId, string TeamName, string VotingName) : IRequest;

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
    
    public class Handler : IRequestHandler<Request>
    {
        private readonly ScrumMasterDbContext _dbContext;
        private readonly ITelegramBotClient _telegramBotClient;

        public Handler(ScrumMasterDbContext dbContext, ITelegramBotClient telegramBotClient)
        {
            _dbContext = dbContext;
            _telegramBotClient = telegramBotClient;
        }

        public async Task Handle(Request request, CancellationToken cancellationToken)
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
                throw new LogicConflictException("User is not a member of the team", UserNotFound);
            }
            
            var voting = new Domain.Models.Voting
            {
                Title = request.VotingName,
                CreatedAt = DateTimeOffset.Now,
                ScrumTeam = team,
                Status = VotingStatus.Created
            };
            
            await _dbContext.Votings.AddAsync(voting, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            for( var i = 0; i < team.Members.Count; i++)
            {
                var member = team.Members[i];
                var message = $"Voting {voting.Title} was started";
                await _telegramBotClient.SendTextMessageAsync(member.User.TelegramUserId, message, cancellationToken: cancellationToken);
            }
        }
    }
}