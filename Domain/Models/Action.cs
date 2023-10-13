using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class Action
{
    public long Id { get; set; }
    
    [Required]
    public long TelegramUserId { get; set; }

    [Required]
    public ActionType Type { get; set; }

    private string? AdditionInfo { set; get; }
    
    public Dictionary<string, string> GetAdditionalData()
    {
        if (AdditionInfo != null)
        {
            return AdditionInfo.Split(';')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x[1]);
        }
        return new Dictionary<string, string>();
    }
    
    public void SetAdditionalData(Dictionary<string, string> data)
    {
        AdditionInfo = string.Join(';', data.Select(x => $"{x.Key}={x.Value}"));
    }
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
public static class AdditionalKeys
{
    public const string ScrumTeamId = "ScrumTeamId";
    public const string ScrumTeamName = "ScrumTeamName";
    public const string VotingId = "VotingId";
}