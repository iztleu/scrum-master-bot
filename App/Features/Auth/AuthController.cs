using App.Features.Auth.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Features.Auth;

[ApiController]
[Route("/auth")]
public class AuthController : Controller
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [AllowAnonymous]
    [HttpGet("send-verify-code")]
    public async Task<IActionResult> SendVerifyCode([FromQuery]SendVerifyCode.Request request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(request, cancellationToken);
        return Ok();
    }
    
    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<Authenticate.Response> Authenticate([FromBody]Authenticate.Request request,
        CancellationToken cancellationToken)
    {
        return await _mediator.Send(request, cancellationToken);
    }
}