using App.Features.Voting.Models;
using App.Features.Voting.Requests;
using App.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Features.Voting;

[ApiController]
[Route("/voting")]
public class VotingController : Controller
{
    private readonly IMediator _mediator;
    private readonly CurrentAuthInfoSource _currentAuthInfoSource;

    public VotingController(IMediator mediator, CurrentAuthInfoSource currentAuthInfoSource)
    {
        _mediator = mediator;
        _currentAuthInfoSource = currentAuthInfoSource;
    }
    
    [HttpPost("start")]
    [Authorize]
    public async Task<IActionResult> StartVoting([FromBody]StartRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new Start.Request(userId, request.TeamName, request.VotingName), cancellationToken);
        return Ok();
    }
    
    [HttpPost("publish")]
    [Authorize]
    public async Task<IActionResult> PublishVoting([FromQuery] long votingId, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new PublishVoting.Request(userId, votingId), cancellationToken);
        return Ok();
    }
    
    [HttpPost("vote")]
    [Authorize]
    public async Task<IActionResult> Vote([FromBody]VoteRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new Vote.Request(userId, request.VotingId, request.Value), cancellationToken);
        return Ok();
    }
    
    [HttpGet("get-active-voting")]
    [Authorize]
    public async Task<IActionResult> GetActiveVoting([FromQuery] GetActiveVotingRequest request,CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        var voting = await _mediator.Send(new GetStartedVoting.Request(userId, request.TeamName), cancellationToken);
        return Ok(voting);
    }
    
    [HttpPost("finish-voting")]
    [Authorize]
    public async Task<IActionResult> FinishVoting([FromBody] FinishVotingRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        await _mediator.Send(new FinishVoting.Request(userId, request.VotingId), cancellationToken);
        return Ok();
    }
}