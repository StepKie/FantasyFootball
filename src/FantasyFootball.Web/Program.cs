using Blazored.LocalStorage;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using FantasyFootball.UI;
using FantasyFootball.UI.ViewModels;
using FantasyFootball.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Serilog;

// Serilog logger for the WASM host. BrowserConsole sink so Log.Debug / Log.Information reach the
// browser DevTools console — the standard config (ISettingsService.StandardLoggerConfig) writes to
// the System.Diagnostics Debug stream + a temp-dir file, neither of which is reachable from a
// browser DevTools session. Without this sink, Log calls land in the void on WASM.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IRepository, LocalStorageRepository>();
builder.Services.AddScoped<ISettingsService, LocalStorageSettingsService>();
builder.Services.AddScoped<IDataService, CsvDataService>();
builder.Services.AddScoped<SettingsViewModel>();
builder.Services.AddScoped<TeamsViewModel>();
builder.Services.AddScoped<TeamDetailViewModel>();
builder.Services.AddScoped<CompetitionsViewModel>();
builder.Services.AddScoped<CompetitionSetupViewModel>();
builder.Services.AddScoped<CompetitionDetailViewModel>();

var app = builder.Build();

// Pre-warm IDataService so the embedded-CSV parse / LocalStorage polymorphic
// deserialize (~200 teams + countries + confederations) runs during the WASM
// boot splash rather than stalling the first navigation to Teams / Competitions.
_ = app.Services.GetRequiredService<IDataService>();

await app.RunAsync();
