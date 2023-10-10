using System.Security.Claims;
using App.Errors.Exceptions;

namespace App.Services;

public class CurrentAuthInfoSource
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentAuthInfoSource(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long GetTelegramUserId()
    {
        var nameIdentifier = _httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(nameIdentifier, out var telegramUserId))
            return telegramUserId;

        throw new ValidationErrorsException(string.Empty, "Could not get user id from claims.", string.Empty);
    }
}
