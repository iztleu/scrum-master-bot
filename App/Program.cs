using App.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        TelegramBotClientOptions options = new("6593045230:AAHHpf_NExM9f6_jDh5-ABQ77ZK3Golv5XY");
        return new TelegramBotClient(options, httpClient);
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
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

app.Run();