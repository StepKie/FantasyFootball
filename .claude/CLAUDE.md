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
background and keep it running. The user can visually inspect progress and
give feedback without being asked. Don't wait to be asked. After a
meaningful chunk lands, point the user at what to look at on the running
site at `https://localhost:7140`.

**Use `dotnet watch`, not `dotnet run`.** Plain `dotnet run` requires a
manual kill + restart for each change *and* a browser hard-refresh (Blazor
WASM caches the runtime / DLLs in the tab; plain F5 won't pick up C#
changes). `dotnet watch` auto-rebuilds on file changes and tells the open
browser tab to reload itself.

```bash
DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true \
  dotnet watch --project src/FantasyFootball.Web run --launch-profile https --non-interactive
```

Three flags / env vars worth understanding:

- **No `--no-hot-reload`** — that disables the browser-refresh signal as
  a side effect, leaving the open tab on the old bundle.
- **`--non-interactive`** — without this, "rude edits" (e.g. changing
  the type of a field) cause `dotnet watch` to prompt
  `Restart? Yes/No/Always/Never` on stdin. The background process can't
  receive that input and the watch hangs — the old WASM bundle keeps
  being served while new code is uncompiled. Symptom: file changes
  silently don't appear in the browser.
- **`DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true`** — answers the rude-edit
  prompt as "Always restart" so the same trap doesn't reopen if
  `--non-interactive` ever stops being respected.

If you see file changes not landing in the browser despite a successful
rebuild log, suspect the rude-edit prompt — kill all `dotnet` processes
(`Get-Process dotnet | Stop-Process -Force`) and relaunch with the env
var + flag above.

If `dotnet watch` exits with code 127 in the background task notifications,
that's the harness reporting the process was killed (e.g. by Stop-Process
when freeing port 7140) — it's not a build failure.

Only ask the user to launch from Visual Studio when interactive debugging
(breakpoints, step-through) is genuinely needed — pure visual inspection
should happen on the watch server you're already running.

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
- **GitHub Pages deployment** is handled by `.github/workflows/github-pages.yml`,
  which auto-deploys on push to `main`. It uses the modern Pages-from-Actions
  artifact pattern (`actions/configure-pages` + `upload-pages-artifact` +
  `deploy-pages`) — no `gh-pages` branch involved. The repo's Pages source must
  be set to "GitHub Actions" in Settings → Pages for the workflow to publish.
- CI build/test (`.github/workflows/dotnet.yml`) runs only on push/PR to `main`.
  PRs to `develop` get the Claude auto-review but not the .NET build — verify
  locally before opening develop-targeted PRs.
