using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class ScrumTeam
{
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    public List<Member> Members { get; set; }
    public DateOnly CreatedAt { get; set; }
    
    public User Owner { get; set; }
}