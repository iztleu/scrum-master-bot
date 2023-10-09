using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

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
    
    public string VerifyCode { get; set; }
}