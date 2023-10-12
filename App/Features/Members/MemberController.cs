using App.Features.Members.Requests;
using App.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Features.Members;

[ApiController]
[Route("/members")]
public class MemberController : Controller
{
    private readonly IMediator _mediator;
    private readonly CurrentAuthInfoSource _currentAuthInfoSource;

    public MemberController(IMediator mediator, CurrentAuthInfoSource currentAuthInfoSource)
    {
        _mediator = mediator;
        _currentAuthInfoSource = currentAuthInfoSource;
    }
    
    [HttpPost("send-invite-request")]
    [Authorize]
    public async Task<IActionResult> SendInviteRequest([FromQuery] string teamName, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new SendInviteRequest.Request(userId, teamName), cancellationToken);
        return Ok();
    }
    
    [HttpPost("accept-invite")]
    [Authorize]
    public async Task<IActionResult> AcceptInvite([FromQuery] int memberId, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new AcceptInvite.Request(userId, memberId), cancellationToken);
        return Ok();
    }
    
    [HttpPost("decline-invite")]
    [Authorize]
    public async Task<IActionResult> DeclineInvite([FromQuery] int memberId, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new DeclineInvite.Request(userId, memberId), cancellationToken);
        return Ok();
    }
    
    [HttpPost("leave-team")]
    [Authorize]
    public async Task<IActionResult> LeaveTeam([FromQuery] string teamName, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new LeaveTeam.Request(userId, teamName), cancellationToken);
        return Ok();
    }
}