using App.Services;
using Database;
using MediatR;

namespace App.Features.ScrumTeam.Requests;

public class JoinScrumTeam
{
    public record Request(int UserId) : IRequest<Response>;

    public record Response();
    
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly ScrumMasterDbContext _dbContext;

        public Handler(ScrumMasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}