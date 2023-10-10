using App.Features.ScrumTeam.Models;
using App.Features.ScrumTeam.Requests;
using App.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Features.ScrumTeam;

[ApiController]
[Route("/scrum-team")]
public class ScrumTeamController : Controller
{
    private readonly IMediator _mediator;
    private readonly CurrentAuthInfoSource _currentAuthInfoSource;

    public ScrumTeamController(IMediator mediator, CurrentAuthInfoSource currentAuthInfoSource)
    {
        _mediator = mediator;
        _currentAuthInfoSource = currentAuthInfoSource;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTeam(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        var team = await _mediator.Send(new CreateScrumTeam.Request(userId, request.TeamName), cancellationToken);
        return Created($"/scrum-team/{team.Name}", team);
    }
    
    [HttpGet("{name}")]
    [Authorize]
    public async Task<IActionResult> GetTeamById(string name, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        var team = await _mediator.Send(new GetScrumTeamById.Request(userId, name), cancellationToken);
        return Ok(team);
    }
    
    [HttpGet()]
    [Authorize]
    public async Task<IActionResult> GetMyTeams(int id, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        var teams = await _mediator.Send(new GetAllMyScrumTeam.Request(userId), cancellationToken);
        return Ok(teams);
    }
    
    [HttpGet("join-to-team")]
    [Authorize]
    public async Task<IActionResult> JoinToTeam([FromQuery] string teamName, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        await _mediator.Send(new JoinToScrumTeam.Request(userId, teamName), cancellationToken);
        return Ok();
    }
    
    [HttpGet("accept-invite")]
    [Authorize]
    public async Task<IActionResult> AcceptInviteToTeam([FromQuery] int memberId, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        await _mediator.Send(new AcceptInviteToScrumTeam.Request(userId, memberId), cancellationToken);
        return Ok();
    }
    
    [HttpGet("decline-invite")]
    [Authorize]
    public async Task<IActionResult> DeclineInviteToTeam([FromQuery] int memberId, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        await _mediator.Send(new DeclineInviteToScrumTeam.Request(userId, memberId), cancellationToken);
        return Ok();
    }
}