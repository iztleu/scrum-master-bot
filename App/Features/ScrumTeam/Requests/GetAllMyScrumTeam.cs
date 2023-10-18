using App.Errors.Exceptions;
using App.Services;
using Database;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Team = Domain.Models.ScrumTeam;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;
using static App.Errors.ValidationErrorsCode;

namespace App.Features.ScrumTeam.Requests;

public class GetAllMyScrumTeam
{
    public record Request(long TelegramUserId) : IRequest<Response>;

    public record Response(Team[] teams);
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var teams = await _dbContext.ScrumTeams
                .AsNoTracking()
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Where(t => t.Members
                    .Any(m => m.User!.TelegramUserId == request.TelegramUserId && m.Status == Status.Accepted))
                .ToArrayAsync(cancellationToken: cancellationToken);

            if (teams == null)
            {
                throw new ValidationErrorsException(string.Empty, "Team not found", TeamNotFound);
            }
            
            return new Response(teams);
        }
    }
}