namespace App.Features.User.Models;

public record CrateUserRequest
{
    public CrateUserRequest(string userName, long telegramUserId)
    {
        if (userName is null) { throw new ArgumentNullException(nameof(userName)); }
     
        UserName = userName;
        ChatId = TelegramUserId = telegramUserId;
    }
    public string UserName {get;}
    public long TelegramUserId { get; }
    public long ChatId { get; }
}