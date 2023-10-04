using System.ComponentModel.DataAnnotations;

namespace Database.Models;

public class User
{
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string UserName { get; set; }
    [Required]
    public long TelegramUserId { get; set; }
    [Required] 
    public long ChatId { get; set; }
}