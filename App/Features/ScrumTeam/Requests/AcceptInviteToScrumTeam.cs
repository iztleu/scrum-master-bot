using App.Errors.Exceptions;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;

namespace App.Features.ScrumTeam.Requests;

public class AcceptInviteToScrumTeam
{
    public record Request(int UserId, int MemberId) : IRequest;
    
    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.UserId)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (x, token) =>
                {
                    var userExists = await dbContext.Users.AnyAsync(user => user.Id == x, token);
                    return userExists;
                }).WithErrorCode(UserNotFound);
            
            RuleFor(x => x.MemberId)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext
                        .Members
                        .AnyAsync(m => m.Id == id && m.Status == Status.Invited, cancellationToken);
                    return teamExist;
                }).WithErrorCode(MemberNotFound);
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
                .Where(t => t.Members
                    .Any(m => m.Id == request.MemberId && m.Status == Status.Invited) && t.Members
                    .Any(m => m.User.Id == request.UserId && m.Role == Role.ScrumMaster))
                .FirstOrDefaultAsync(cancellationToken);
            
            if (team == null)
            {
                throw new LogicConflictException( "Member not found", MemberNotFound);
            }
          
            var member = team.Members.FirstOrDefault(m => m.Id == request.MemberId);
            member!.Status = Status.Accepted;
           
          
            await _telegramBotClient.SendTextMessageAsync(
                member.User.ChatId,
                $"You have been accepted to the team {team.Name}",
                cancellationToken: cancellationToken
            );
            
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}