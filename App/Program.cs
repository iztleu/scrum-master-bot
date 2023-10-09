using System.Reflection;
using App;
using App.Features.Auth.Registration;
using App.Pipeline.Behaviors;
using App.Errors.Extensions;
using App.Services;
using Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddMediatR(cfg => cfg
    .RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
    .AddOpenBehavior(typeof(LoggingBehavior<,>))
    .AddOpenBehavior(typeof(ValidationBehavior<,>)));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddScoped<CurrentAuthInfoSource>();

builder.AddAuth();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        TelegramBotClientOptions options = new(builder.Configuration.GetRequiredSection("Token")!.Value!);
        return new TelegramBotClient(options, httpClient);
    });

builder.AddDbContext();
builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
builder.Services.AddScoped<IReceiverService, ReceiverService>();
builder.Services.AddHostedService<TelegramBotService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapProblemDetails();

RunMigration(app);

app.Run();


void RunMigration(WebApplication webApplication)
{
    using (var scope = webApplication.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ScrumMasterDbContext>();
        
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            dbContext.Database.Migrate();
    }
}