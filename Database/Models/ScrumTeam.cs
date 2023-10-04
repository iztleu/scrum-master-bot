using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public class ScrumTeam
{
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    public Member[] Members { get; set; }
    public DateOnly CreatedAt { get; set; }
}