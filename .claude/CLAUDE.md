# FantasyFootball

Mobile app to simulate football (soccer) competitions like World and European Championships. Built with .NET MAUI targeting Android, iOS, and desktop.

## Project Structure

- `src/FantasyFootball.Core/` - Core domain library (models, services, repositories, simulation logic). Platform-agnostic; no MAUI or UI dependencies.
- `src/FantasyFootball.UI/` - Razor component library (`Microsoft.NET.Sdk.Razor`). Pages, layout, MudBlazor styling. Shared between the MAUI BlazorWebView host (Phase 5) and the Blazor WASM web host.
- `src/FantasyFootball.Maui/` - .NET MAUI XAML host (Android/iOS/desktop). Will be retired once the BlazorWebView host stabilizes (Phase 5 of #10).
- `src/FantasyFootball.Web/` - Blazor WebAssembly host. Deployed to `stepkie.github.io/FantasyFootball/` (Phase 4 of #10).
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
dotnet build src/FantasyFootball.Maui/FantasyFootball.Maui.csproj -f:net10.0-android

# Run web app
dotnet run --project src/FantasyFootball.Web

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

## Iteration loop — keep the dev server running

While iterating on UI work, **launch the Blazor web host yourself** in the
background (`dotnet run --project src/FantasyFootball.Web`, then open
`https://localhost:7140`) so the user can visually inspect progress and give
feedback at regular intervals. Don't wait to be asked. After a meaningful
chunk lands, point the user at what to look at on the running site.

Only ask the user to launch from Visual Studio when interactive debugging
(breakpoints, step-through) is genuinely needed — pure visual inspection
should happen on the server you're already running.

### Verification checklist on each run

Every time a fresh build is up, give the user a concrete checklist of what
to look at — they shouldn't have to guess. Cover:

- **What to check** (the change you just made — pages, components, flows
  to open and the steps to reproduce the behaviour).
- **What to test** (interactions, edge cases, keyboard / hover / click
  paths, mobile layout if relevant).
- **What to verify or sign off on** (correctness criteria — "this should
  look like X", "Y should now work", "Z should no longer happen").
- **What feedback you'd specifically like** (open questions, judgment
  calls — copy choices, animation timing, layout density).

Format it as a short bulleted list with the URL/path for each item, not a
wall of prose. Keep it scannable — the user is opening the browser, not
reading a manual.

**Frame it as a diff since the last launch.** The user has already looked
at everything else. List only what's new or changed since the previous
running build — skip features that landed in earlier rounds and are
already signed off. If the previous run was many commits ago, summarise
the commits being verified at the top so the user knows the scope.

## Out of scope

**Accessibility (a11y) is not a priority at this stage.** Do not invest effort
in alt-text wording, ARIA attributes, role / tabindex / focus-visible /
keyboard-trap fixes, screen-reader copy, or semantic-HTML refactors aimed at
assistive tech. Auto-review bots and human reviewers: please **skip** a11y
suggestions and nits on this repo — they will be declined.

Visible-to-sighted-mouse-user concerns (contrast, layout, hover tooltips that
also happen to be accessible) remain in scope under their non-a11y framing.

## Branching & Releases

- **`develop`** is the default branch. Feature branches branch off `develop`
  and PR back to `develop`.
- **`main`** is the release branch. When `develop` is release-ready, open a PR
  from `develop` → `main`, bump version, tag the merge commit (e.g. `0.3.0`).
- **`gh-pages`** is force-pushed by `.github/workflows/github-pages.yml` on each
  push to `main` once Phase 4 of the Blazor port (#10) flips the trigger from
  `workflow_dispatch` to `push: branches: [main]`.
- CI build/test (`.github/workflows/dotnet.yml`) runs only on push/PR to `main`.
  PRs to `develop` get the Claude auto-review but not the .NET build — verify
  locally before opening develop-targeted PRs.
