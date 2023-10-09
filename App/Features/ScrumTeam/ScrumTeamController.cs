using App.Features.ScrumTeam.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Features.ScrumTeam;

[ApiController]
[Route("/scrum-team")]
public class ScrumTeamController : Controller
{
    private readonly IMediator _mediator;

    public ScrumTeamController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //create team endpoint
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTeam(CreateScrumTeam.Request request)
    {
        var team = await _mediator.Send(request);
        return Created($"/scrum-team/{team.Id}", team);
    }
    
    //get team endpoint by id
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTeamById(int id)
    {
        var team = await _mediator.Send(new GetScrumTeamById.Request(id));
        return Ok(team);
    }
}