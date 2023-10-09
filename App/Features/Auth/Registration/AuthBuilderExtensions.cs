using App.Features.Auth.Options;
using App.Features.Auth.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace App.Features.Auth.Registration;

public static class AuthBuilderExtensions
{
    public static WebApplicationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection($"Features:{AuthOptions.SectionName}");
        builder.Services.Configure<AuthOptions>(section);
        
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            var jwt = section.Get<AuthOptions>()?.Jwt;
            if (jwt is null)
            {
                throw new InvalidOperationException("Jwt settings not found");
            }
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
            };
        });

        builder.Services.AddAuthorization();
        builder.Services.AddTransient<TokenService>();
        
        return builder;
    }
}