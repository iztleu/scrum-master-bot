using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public class Action
{
    public long Id { get; set; }

    [Required]
    public int UserId { get; set; }
    
    public User User { get; set; }

    [Required]
    public ActionType Type { get; set; }

    public string? AdditionInfo { get; set; }
}

public enum ActionType
{
    CreateScrumTeam,
}