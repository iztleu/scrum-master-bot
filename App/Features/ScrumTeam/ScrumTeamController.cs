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
        return Created($"/scrum-team/{team.Id}", team);
    }
    
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTeamById(int id, CancellationToken cancellationToken)
    {
        var userId = _currentAuthInfoSource.GetUserId();
        var team = await _mediator.Send(new GetScrumTeamById.Request(userId,id), cancellationToken);
        return Ok(team);
    }
}