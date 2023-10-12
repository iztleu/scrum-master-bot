namespace Domain.Models;

public class Voting
{
    public long Id { get; set; }
    
    public string Title { get; set; }
    
    public int ScrumTeamId { get; set; }
    
    public ScrumTeam ScrumTeam { get; set; }
    
    public VotingStatus Status { get; set; }
    
    public Vote[] Votes { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}

public enum VotingStatus
{
    Created,
    Started,
    Finished
}