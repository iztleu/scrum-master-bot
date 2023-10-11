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
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        var team = await _mediator.Send(new CreateScrumTeam.Request(userId, request.TeamName), cancellationToken);
        return Created($"/scrum-team/{team.Name}", team);
    }
    
    [HttpGet("{name}")]
    [Authorize]
    public async Task<IActionResult> GetTeamByName(string name, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        var team = await _mediator.Send(new GetScrumTeamByName.Request(userId, name), cancellationToken);
        return Ok(team);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyTeams(CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        var teams = await _mediator.Send(new GetAllMyScrumTeam.Request(userId), cancellationToken);
        return Ok(teams);
    }
    
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> RenameTeam([FromBody]RenameTeamRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetTelegramUserId();
        var team = await _mediator.Send(new RenameScrumTeam.Request(userId, request.OldTeamName, request.NewTeamName), cancellationToken);
        return Ok(team);
    }
}