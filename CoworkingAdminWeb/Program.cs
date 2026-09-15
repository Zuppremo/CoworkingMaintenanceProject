using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CoworkingAdminWeb;
using CoworkingAdminWeb.Services;

using CoworkingAdminWeb.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<JwtAuthorizationMessageHandler>();

builder.Services.AddHttpClient("API", client => client.BaseAddress = new Uri("https://localhost:7200/"))
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

builder.Services.AddScoped<GridEditorState>();

await builder.Build().RunAsync();