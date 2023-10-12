using App.Errors.Exceptions;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using static App.Features.Member.Errors.MembersValidationErrors;

namespace App.Features.Members.Requests;

public class DeclineInvite
{
        public record Request(long TelegramUserId, int MemberId) : IRequest;
    
    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (x, token) =>
                {
                    var userExists = await dbContext.Users.AsNoTracking().AnyAsync(user => user.TelegramUserId == x, token);
                    return userExists;
                }).WithErrorCode(UserNotFound);
            
            RuleFor(x => x.MemberId)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext
                        .Members
                        .AsNoTracking()
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
                    .Any(m => m.User!.TelegramUserId == request.TelegramUserId && m.Role == Role.ScrumMaster))
                .FirstOrDefaultAsync(cancellationToken);
            
            if (team == null)
            {
                throw new LogicConflictException( "Member not found", MemberNotFound);
            }
          
            var member = team.Members.FirstOrDefault(m => m.Id == request.MemberId);
          
            await _telegramBotClient.SendTextMessageAsync(
                team.Members.FirstOrDefault(m => m.Role == Role.ScrumMaster)?.User.ChatId??member.User.ChatId,
                $"User {member.User!.UserName} declined invitation to team {team.Name}",
                cancellationToken: cancellationToken
            );
          
            await _telegramBotClient.SendTextMessageAsync(
                member.User!.ChatId,
                $"You declined invitation to team {team.Name}",
                cancellationToken: cancellationToken
            );
            
            _dbContext.Remove(member);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}