using Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.TelegramUserId == request.TelegramUserId, cancellationToken);
            return new Response(user);
        }
    }
}