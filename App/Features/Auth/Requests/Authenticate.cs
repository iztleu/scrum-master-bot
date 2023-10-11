using App.Errors.Exceptions;
using App.Features.Auth.Services;
using Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using static App.Features.Auth.Errors.AuthValidationErrors;

namespace App.Features.Auth.Requests;

public class Authenticate
{
    public record Request(string UserName, string VerifyCode) : IRequest<Response>;
    public record Response(string AccessToken);
    
    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.UserName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(UserNameRequired);
        }
    }
    
    public class Handler : IRequestHandler<Request, Response>
    {
        
        private readonly ScrumMasterDbContext _dbContext;
        private readonly TokenService _tokenService;

        public Handler(ScrumMasterDbContext dbContext, TokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = await _dbContext
                .Users
                .Where(x => x.UserName == request.UserName 
                             && x.VerifyCode == request.VerifyCode
                             && x.VerifyCode != "" )
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);
            
            if (user == null)
            {
                throw new ValidationErrorsException(string.Empty, "Wrong UserName or VerifyCode", WrongCredentials);
            }

            user.VerifyCode = "";
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            var token = _tokenService.CreateAccessToken(user);
            
            return new Response(token);
        }
    }
}