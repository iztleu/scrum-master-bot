
using App.Resources;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Team = Domain.Models.ScrumTeam;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;
using static App.Errors.ValidationErrorsCode;

namespace App.Features.ScrumTeam.Requests;

public class CreateScrumTeam
{
    public record Request(long TelegramUserId, string Name) : IRequest<Response>;

    public record Response(string Name);

    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams
                        .AsNoTracking()
                        .AnyAsync(t => t.Owner.TelegramUserId == id, cancellationToken);
                    return !teamExist;
                })
                .WithErrorCode(UserAlreadyHasTeam.WithPrefix())
                .WithMessage(UserAlreadyHasTeam);
            
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams
                        .AsNoTracking()
                        .AnyAsync(t => t.Name == name, cancellationToken);
                    return !teamExist;
                }).WithErrorCode(TeamAlreadyExists.WithPrefix())
                .WithMessage(TeamAlreadyExists);
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
            var user = await _dbContext.Users.Where(u => u.TelegramUserId == request.TelegramUserId)
                .FirstAsync(cancellationToken);
         
            var scrumTeam = new Domain.Models.ScrumTeam
            {
                Name = request.Name,
                CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                Members = new List<Domain.Models.Member> {new ()
                {
                    User = user,
                    Role = Role.ScrumMaster,
                    Status = Status.Accepted
                }},
                Owner = user
            };

            await _dbContext.ScrumTeams.AddAsync(scrumTeam, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Response(scrumTeam.Name);
        }
    }
}