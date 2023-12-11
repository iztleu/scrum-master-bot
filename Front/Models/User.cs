using System.Security.Claims;

namespace Front.Models;

public class User
{
    public string Username { get; set; } = "";
    
    public long UserId { get; set; }

    public ClaimsPrincipal ToClaimsPrincipal() => new(new ClaimsIdentity(new Claim[]
        {
            new (ClaimTypes.Name, Username),
            new (ClaimTypes.NameIdentifier, UserId.ToString()),
        },
        "ScrumMaterBotIdentity"));

    public static User FromClaimsPrincipal(ClaimsPrincipal principal) => new()
    {
        Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
        UserId = Convert.ToInt64(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value),
    };
}