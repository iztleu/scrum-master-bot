using App.Errors.Exceptions;
using App.Services;
using Database;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Team = Domain.Models.ScrumTeam;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;

namespace App.Features.ScrumTeam.Requests;

public class GetScrumTeamById
{
    public record Request(int UserId, string TeamName) : IRequest<Response>;

    public record Response(Team Team);
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;
        private readonly CurrentAuthInfoSource _currentAuthInfoSource;

        public Handler(ScrumMasterDbContext dbContext, CurrentAuthInfoSource currentAuthInfoSource)
        {
            _dbContext = dbContext;
            _currentAuthInfoSource = currentAuthInfoSource;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var userId = _currentAuthInfoSource.GetUserId();
            var team = await _dbContext.ScrumTeams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Members.Any(m => m.User.Id == userId && m.Status == Status.Accepted) && t.Name == request.TeamName)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                throw new ValidationErrorsException(string.Empty, "Team not found", TeamNotFound);
            }
            
            return new Response(team);
        }
    }
}