using App.Errors.Exceptions;
using App.Services;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Team = Domain.Models.ScrumTeam;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;

namespace App.Features.ScrumTeam.Requests;

public class CreateScrumTeam
{
    public record Request(string Name) : IRequest<Response>;

    public record Response(int Id);

    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams.AnyAsync(t => t.Name == name, cancellationToken);
                    return !teamExist;
                }).WithErrorCode(TeamAlreadyExists);
        }
    }
    
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
            var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new LogicConflictException( "User not found", UserNotFound);
            }
            
            var scrumTeam = new Domain.Models.ScrumTeam
            {
                Name = request.Name,
                CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                Members = new List<Member> {new ()
                {
                    User = user,
                    Role = Role.ScrumMaster,
                }}
            };

            await _dbContext.ScrumTeams.AddAsync(scrumTeam, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Response(scrumTeam.Id);
        }
    }
}