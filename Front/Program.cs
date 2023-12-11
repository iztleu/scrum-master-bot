using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Front;
using Front.Providers;
using Front.Services;
using Front.Storage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("app",
    _ => new HttpClient { BaseAddress = new Uri("http://localhost:5217") });

builder.Services.AddScoped<MemoryStorage>();
builder.Services.AddScoped<ScrumMusterBotUserService>();
builder.Services.AddScoped<ScrumMasterBotAuthenticationStateProvider>();

builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ScrumMasterBotAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();