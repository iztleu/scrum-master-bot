using App.Errors.Exceptions;
using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using static App.Features.Auth.Errors.AuthValidationErrors;

namespace App.Features.Auth.Requests;

public class SendVerifyCode
{
    public record Request(string UserName) : IRequest<Response>;

    public record Response();

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator(ScrumMasterDbContext _dbContext)
        {
            RuleFor(x => x.UserName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(UserNameRequired)
                .MustAsync(async (x, token) =>
                {
                    var userExists = await _dbContext.Users.AnyAsync(user => user.UserName == x, token);
                    return userExists;
                }).WithErrorCode(UserNameDoesNotExist);
        }
    }

    public class Handler : IRequestHandler<Request, Response>
    {

        private readonly ScrumMasterDbContext _dbContext;
        private readonly ITelegramBotClient _telegramBotClient;

        public Handler(ScrumMasterDbContext dbContext, ITelegramBotClient telegramBotClient)
        {
            _dbContext = dbContext;
            _telegramBotClient = telegramBotClient;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = await _dbContext
                .Users
                .Where(x => x.UserName == request.UserName)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (user == null)
            {
                throw new ValidationErrorsException(string.Empty, "Wrong UserName or VerifyCode", WrongCredentials);
            }

            var verifyCode = new Random().Next(0, 9999);
            user.VerifyCode = verifyCode.ToString("0000");
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _telegramBotClient.SendTextMessageAsync(user.ChatId, $"Your verify code is {user.VerifyCode}",
                cancellationToken: cancellationToken);

            return new Response();
        }
    }
}