# FantasyFootball

Mobile app to simulate football (soccer) competitions like World and European Championships. Built with .NET MAUI targeting Android, iOS, and desktop.

## Project Structure

- `src/FantasyFootball/` - Core library (models, services, repositories, data)
- `src/FantasyFootball.UI/` - .NET MAUI UI application (Android/iOS/desktop)
- `src/FantasyFootball.Tests/` - xUnit v3 test project

## Tech Stack

- .NET 10.0, C#, .NET MAUI
- SQLite for local data storage (sqlite-net-pcl, SQLiteNetExtensions)
- CommunityToolkit.Mvvm for MVVM pattern
- CommunityToolkit.Maui for UI helpers
- CsvHelper for CSV data import (FIFA ELO rankings)
- MathNet.Numerics for statistical computations
- Serilog for logging
- xUnit v3 + AwesomeAssertions for tests

## Commands

```bash
# Build (general)
dotnet build FantasyFootball.sln

# Build Android
dotnet build src/FantasyFootball.UI/FantasyFootball.UI.csproj -f:net10.0-android

# Run tests
dotnet test src/FantasyFootball.Tests/FantasyFootball.Tests.csproj

# Install MAUI workload (if needed)
dotnet workload install maui
```

## Conventions

- File-scoped namespaces
- 4 spaces indentation for C#, 2 spaces for XML/csproj/XAML
- Allman brace style (opening brace on new line)
- PascalCase for types and public members
- MVVM pattern: ViewModels in UI project, Models/Services in core library
- XAML formatting governed by Settings.XamlStyler
- Follow .editorconfig rules
- Default branch is `master`
