namespace App.Features.ScrumTeam.Models;

public record CreateTeamRequest(string TeamName);

public record RenameTeamRequest(string OldTeamName, string NewTeamName);