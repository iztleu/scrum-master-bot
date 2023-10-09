using Database;
using Docker.DotNet;
using Microsoft.EntityFrameworkCore;

namespace App;

public static class DbBuilderExtensions
{
    public static WebApplicationBuilder AddDbContext(this WebApplicationBuilder builder)
    {
        // if (builder.Environment.IsDevelopment())
        // {
        //     DockerClient client = new DockerClientConfiguration(
        //             new Uri("http://ubuntu-docker.cloudapp.net:4243"))
        //         .CreateClient();
        // }
        //
        builder.Services
            .AddDbContext<ScrumMasterDbContext>(
                optionsBuilder => optionsBuilder.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseAdminDatabase("postgres")));
        
        return builder;
    }
}