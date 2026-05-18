using Blazored.LocalStorage;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using FantasyFootball.UI;
using FantasyFootball.UI.ViewModels;
using FantasyFootball.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

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

var app = builder.Build();

// Pre-warm IDataService so the embedded-CSV parse / LocalStorage polymorphic
// deserialize (~200 teams + countries + confederations) runs during the WASM
// boot splash rather than stalling the first navigation to Teams / Competitions.
_ = app.Services.GetRequiredService<IDataService>();

await app.RunAsync();
