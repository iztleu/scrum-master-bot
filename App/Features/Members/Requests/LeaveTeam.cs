using App.Errors.Exceptions;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static App.Features.Member.Errors.MembersValidationErrors;

namespace App.Features.Members.Requests;

public class LeaveTeam
{
    public record Request(long TelegramUserId, string TeamName) : IRequest;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (x, token) =>
                {
                    var userExists = await dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(user => user.TelegramUserId == x, token);
                    return userExists;
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
    
    public class Handler : IRequestHandler<Request>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(Request request, CancellationToken cancellationToken)
        {
            var team = await _dbContext.ScrumTeams
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Name == request.TeamName, cancellationToken);

            if (team.Owner.TelegramUserId == request.TelegramUserId)
            {
                throw new LogicConflictException("You can't leave team, because you are owner of this team", OwnerCannotLeaveTeam);
            }
            
            var member = team.Members.FirstOrDefault(m => m.User.TelegramUserId == request.TelegramUserId);
            team.Members.Remove(member);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}