using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CfPerfCalc;
using CfPerfCalc.Services;
using CfPerfCalc.Services.FormulaOptimizer;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CfApiClient>();
builder.Services.AddScoped<JSInterop>();
builder.Services.AddScoped<RatingCache>();
builder.Services.AddScoped<PerfCache>();
builder.Services.AddScoped<UserHandleCache>();
builder.Services.AddScoped<FormulaOptimizationService>();


await builder.Build().RunAsync();