using App.Errors.Exceptions;
using Database;
using Domain.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using static App.Features.ScrumTeam.Errors.ScrumTeamValidationErrors;

namespace App.Features.ScrumTeam.Requests;

public class JoinToScrumTeam
{
    public record Request(long TelegramUserId, string TeamName) : IRequest;
    
    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.TelegramUserId)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (x, token) =>
                {
                    var userExists = await dbContext.Users.AnyAsync(user => user.TelegramUserId == x, token);
                    return userExists;
                }).WithErrorCode(UserNotFound);
            
            RuleFor(x => x.TeamName)
                .NotEmpty()
                .WithErrorCode(NameRequired)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.ScrumTeams.AnyAsync(t => t.Name == name, cancellationToken);
                    return teamExist;
                }).WithErrorCode(TeamNotFound);
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
                .FirstOrDefaultAsync(t => t.Name == request.TeamName, cancellationToken);
            
            if (team == null)
            {
                throw new LogicConflictException( "Team not found", TeamNotFound);
            }

            var user = await _dbContext
                .Users
                .Where(u => u.TelegramUserId == request.TelegramUserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                throw new LogicConflictException( "User not found", UserNotFound);
            }

            if (team.Members.Any(m => m.User!.TelegramUserId == request.TelegramUserId))
            {
                throw new LogicConflictException( "User already in team", UserAlreadyInTeam);
            }
            
            var scrumMasterUser = team.Members.FirstOrDefault(m => m.Role == Role.ScrumMaster)?.User;
            if (scrumMasterUser == null)
            {
                throw new LogicConflictException( "ScrumMaster not found", ScrumMasterNotFound);
            }
            
            var newMembers = new Member
            {
                User = user,
                Role = Role.Developer,
                Status = Status.Invited,
            };
            team.Members.Add(newMembers);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _telegramBotClient.SendTextMessageAsync(
                scrumMasterUser.ChatId,
                $"User {user.UserName} want to join to your team {team.Name}",
                replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("Accept", $"accept invite request {newMembers.Id}"),
                    InlineKeyboardButton.WithCallbackData("Decline", $"decline invite request {newMembers.Id}"),
                }),
                cancellationToken: cancellationToken
            );
            
            
        }
    }
}