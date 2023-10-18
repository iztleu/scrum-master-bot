using App.Errors.Exceptions;
using App.Services;
using Database;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Team = Domain.Models.ScrumTeam;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;
using static App.Errors.ValidationErrorsCode;

namespace App.Features.ScrumTeam.Requests;

public class GetScrumTeamByName
{
    public record Request(long TelegramUserId, string TeamName) : IRequest<Response>;

    public record Response(Team Team);
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext, IStringLocalizer<SharedResource> localizer)
        {
            _dbContext = dbContext;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var team = await _dbContext.ScrumTeams
                .AsNoTracking()
                .Include(t => t.Members.Where(m => m.Status == Status.Accepted))
                .ThenInclude(m => m.User)
                .Where(t => t.Members.Any(m => m.User!.TelegramUserId == request.TelegramUserId) && t.Name == request.TeamName)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                throw new LogicConflictException(TeamNotFound, TeamNotFound.WithPrefix());
            }
            
            return new Response(team);
        }
    }
}