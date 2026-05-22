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

// Serilog → browser DevTools console for the WASM host. Debug in dev; Warning in Release (avoids leaking diagnostics to public-deploy visitors' consoles).
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(
#if DEBUG
        Serilog.Events.LogEventLevel.Debug
#else
        Serilog.Events.LogEventLevel.Warning
#endif
    )
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

// New flat-model services. Wired in alongside the old graph path; pages
// migrate to these in subsequent phases of PR #2 (UI cutover).
builder.Services.AddSingleton<ICompetitionDefinitionStore, EmbeddedCompetitionDefinitionStore>();
builder.Services.AddScoped<IFlatTeamRegistry, DataServiceTeamRegistry>();
builder.Services.AddScoped<IFlatCompetitionRepository, LocalStorageFlatCompetitionRepository>();
builder.Services.AddScoped<FlatCompetitionFactory>();
builder.Services.AddScoped<IScoreModel, EloScoreModel>();
builder.Services.AddScoped<FlatCompetitionSimulator>();
builder.Services.AddScoped<BulkSimRunner>();

builder.Services.AddScoped<SettingsViewModel>();
builder.Services.AddScoped<TeamsViewModel>();
builder.Services.AddScoped<TeamDetailViewModel>();
builder.Services.AddScoped<FlatCompetitionDetailViewModel>();
builder.Services.AddScoped<FlatCompetitionSetupViewModel>();
builder.Services.AddScoped<FlatCompetitionsViewModel>();

var app = builder.Build();

// Pre-warm IDataService so the embedded-CSV parse / LocalStorage polymorphic
// deserialize (~200 teams + countries + confederations) runs during the WASM
// boot splash rather than stalling the first navigation to Teams / Competitions.
_ = app.Services.GetRequiredService<IDataService>();

await app.RunAsync();
