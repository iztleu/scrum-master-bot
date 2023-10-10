using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class Member
{
    public int Id { get; set; }
    [Required]
    public User? User { get; set; }
    [Required]
    public Role Role { get; set; }
    
    public Status Status { get; set; }
}

public enum Status
{
    Invited,
    Accepted,
    Declined,
    Removed,
}