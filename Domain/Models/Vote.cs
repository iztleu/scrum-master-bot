namespace Domain.Models;

public class Vote
{
    public long Id { get; set; }
    
    public Member Member { get; set; }
    
    public string Value { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}