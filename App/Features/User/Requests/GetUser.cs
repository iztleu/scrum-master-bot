using Database;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using static App.Features.User.Errors.UserValidationErrors;

namespace App.Features.User.Requests;

public class GetUser
{
    public record Request(long TelegramUserId): IRequest<Response>;
    public record Response(Domain.Models.User? User);
    
    // public class RequestValidator: AbstractValidator<Request>
    // {
    //     public RequestValidator(ScrumMasterDbContext dbContext)
    //     {
    //         RuleFor(x => x.TelegramUserId)
    //             .MustAsync(async (id, cancellationToken) =>
    //             {
    //                 var teamExist = await dbContext.Users
    //                     .AnyAsync(u => u.TelegramUserId == id, cancellationToken);
    //                 return teamExist;
    //             })
    //             .WithErrorCode(UserNotFound);
    //     }
    // }
    
    public class Handler(ScrumMasterDbContext _dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.TelegramUserId == request.TelegramUserId, cancellationToken);
            return new Response(user);
        }
    }
}


public class HomeController(ILogger<HomeController> _logger, ScrumMasterDbContext _dbContext) : Controller
{
    
}

