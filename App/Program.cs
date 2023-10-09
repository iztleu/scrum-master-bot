using App;
using App.Services;
using Database;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

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