using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public class Member
{
    public int Id { get; set; }
    [Required]
    public User User { get; set; }
    [Required]
    public Role Role { get; set; }
}