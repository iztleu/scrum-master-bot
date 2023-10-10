using System.Security.Claims;
using App.Features.Auth.Options;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace App.Features.Auth.Services;

public class TokenService
{
    private readonly AuthOptions.JwtOptions _jwtOptions;
    
    public TokenService(IOptions<AuthOptions> options)
    {
        _jwtOptions = options.Value.Jwt;
    }

    public string CreateAccessToken(User user)
    {
        return CreateAccessToken(user.TelegramUserId, user.UserName);
    }
    
    public string CreateAccessToken(long userId, string userName)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.SigningKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var utcNow = DateTime.UtcNow;

        
        
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            Expires = utcNow.Add(_jwtOptions.Duration),
            NotBefore = utcNow,
            SigningCredentials = signingCredentials,
            Claims = new Dictionary<string, object>
            {
                { ClaimTypes.NameIdentifier, userId },
                { ClaimTypes.Name, userName },
            },
        };

        var accessToken = new JsonWebTokenHandler().CreateToken(securityTokenDescriptor);

        return accessToken;
    }
}