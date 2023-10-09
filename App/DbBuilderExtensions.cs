using Database;
using Microsoft.EntityFrameworkCore;

namespace App;

public static class DbBuilderExtensions
{
    public static WebApplicationBuilder AddDbContext(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDbContext<ScrumMasterDbContext>(
                optionsBuilder => optionsBuilder.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseAdminDatabase("postgres")), ServiceLifetime.Transient);
        
        return builder;
    }
}