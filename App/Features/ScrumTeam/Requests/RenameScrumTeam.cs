using App.Errors.Exceptions;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;

namespace App.Features.ScrumTeam.Requests;

public class RenameScrumTeam
{
    public record Request(long TelegramUserId, string OldTeamName, string NewTeamName) : IRequest<Response>;
    public record Response(Domain.Models.ScrumTeam Team);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams
                        .AsNoTracking()
                        .AnyAsync(t => t.Members
                            .Any(m => m.User.TelegramUserId == id && m.Role == Role.ScrumMaster), 
                            cancellationToken);
                    return teamExist;
                }).WithErrorCode(TeamNotFound);
            
            RuleFor(x => x.OldTeamName)
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
            
            RuleFor(x => x.NewTeamName)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams
                        .AsNoTracking()
                        .AnyAsync(t => t.Name == name, 
                        cancellationToken);
                    return !teamExist;
                }).WithErrorCode(NameAlreadyTaken);
        }
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
            var team = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Name == request.OldTeamName)
                .FirstAsync(cancellationToken);
            if (team == null)
            {
                throw new ValidationErrorsException(string.Empty, "Team not found", TeamNotFound);
            }
            
            if(team.Members.Any(m => m.User.TelegramUserId == request.TelegramUserId && m.Role != Role.ScrumMaster))
            {
                throw new ValidationErrorsException(string.Empty, "You are not scrum master", UserIsNotScrumMaster);
            }
            
            team.Name = request.NewTeamName;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return new Response(team);
        }
    }
}