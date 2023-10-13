using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class Action
{
    public long Id { get; set; }
    
    [Required]
    public long TelegramUserId { get; set; }

    [Required]
    public ActionType Type { get; set; }

    public string? AdditionInfo { get; set; }
}

public enum ActionType
{
    CreateScrumTeam,
    ShowAllTeams,
    JoinToScrumTeam,
    ChooseScrumTeamActions,
    RenameScrumTeam,
    StartVoting,
}