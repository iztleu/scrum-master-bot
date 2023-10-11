using App.Features.User.Models;
using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static App.Features.User.Errors.UserValidationErrors;
namespace App.Features.User.Requests;

public class ReqistrateUser
{
    public record Request(CrateUserRequest UserModel) : IRequest<Response>;
    public record Response(string Name);

    public class RequestValidator: AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext dbContext)
        {
            RuleFor(x => x.UserModel.UserName)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var teamExist = await dbContext.Users
                        .AnyAsync(u => u.UserName == name, cancellationToken);
                    return !teamExist;
                })
                .WithErrorCode(AlreadyExists);
            
            RuleFor(x => x.UserModel.TelegramUserId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var teamExist = await dbContext.Users
                        .AnyAsync(u => u.TelegramUserId == id, cancellationToken);
                    return !teamExist;
                })
                .WithErrorCode(AlreadyExists);
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
            var user = new Domain.Models.User
            {
                UserName = request.UserModel.UserName,
                TelegramUserId = request.UserModel.TelegramUserId,
                ChatId = request.UserModel.ChatId,
                VerifyCode = string.Empty
            };
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new Response(user.UserName);
        }
    }
}