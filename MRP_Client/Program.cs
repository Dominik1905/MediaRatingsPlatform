using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MRP_Client;
using MRP_Client.Services;
using Microsoft.AspNetCore.Components.Web;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();