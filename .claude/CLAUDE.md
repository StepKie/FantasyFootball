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

## Test speed — fast is the default

`CompetitionSimulator`'s constructor defaults to `msGameDelay = 0`
(instant — no inter-game delay). UI callers that want visible pacing
(Slow / Normal / Fast speed picker) opt in explicitly. **Tests just
use the default** — no special argument needed.

```csharp
// ✅ tests — default is instant
var simulator = new CompetitionSimulator(competition, Repo);

// ✅ UI — opt into visible pacing
var simulator = new CompetitionSimulator(competition, Repo, msGameDelay: 100);
// or the Blazor pattern: construct + .GameDelay = Speed.ToDelay() in ApplySpeedToSimulator().
```

**Design principle behind the default:** the constructor should give
you the fastest thing that does the job. Slowdown is a presentation
concern; opt in to it. Defaults are how APIs encode "what should
happen if you don't think about it" — and the answer for a simulator
is *not* "wait 100ms between games."

**Plain `dotnet test` should complete in well under a minute.** If a
test is genuinely long-running for unavoidable reasons (50+ randomised
property runs that have to be a property test, not a hot-path unit
test), tag it `[Trait("Category", "Slow")]` and document the filter
(`dotnet test --filter "Category!=Slow"`) in this file. Today no test
needs that escape hatch; if you add one that does, add the section
before merging.

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

## PR merge protocol

This project follows the **Branch Merge Protocol** in the global
`~/.claude/CLAUDE.md` — topical commits prepared locally, then
`gh pr merge --merge` for the GitHub merge. See that section for the
full mechanics + rationale.

Project-specific notes:

- Reference PRs that landed with this protocol: #25, #28, #29, #36.
- Both `feature → develop` AND `develop → main` use the same protocol.
- For `develop → main`: bump version + tag the merge commit (`X.Y.Z`)
  after merge — see "Branching & Releases" below.

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
- **`main`** is the release branch. When `develop` is release-ready, merge
  `develop` → `main` **directly** (no PR — everything was already reviewed on
  its way into `develop`), then tag the merge commit (e.g. `0.6.0`). Auto-review
  is **disabled** at the workflow level on PRs targeting `main` (see
  `claude-auto-review` in `.github/workflows/claude.yml`) — left in place as
  belt-and-braces in case a PR is ever opened.
- **Version bump** lives in `Directory.Build.props` at the repo root (single
  source of truth — every `.csproj` inherits it). The .NET SDK auto-derives
  `AssemblyInformationalVersionAttribute` from `<Version>` and appends
  `+<git-sha>` when a git working copy is present. The toolbar's "v{version} ·
  {commit}" link reads this attribute at runtime.
- **`CHANGELOG.md`** at the repo root tracks releases in user-facing language
  (see `[[feedback-changelog-user-facing]]`). The Web project stages it into
  `wwwroot/` via a build target so the `/whats-new` page can fetch + render it.

### Release workflow

Cutting `X.Y.Z` from `develop`:

1. **Release-prep PR to `develop`** (mandatory — never tag without it):
   - Bump `<Version>` in `Directory.Build.props` to `X.Y.Z`.
   - Add a new `## [X.Y.Z](https://github.com/StepKie/FantasyFootball/releases/tag/X.Y.Z) — YYYY-MM-DD`
     section at the top of `CHANGELOG.md` with user-facing notes (New Features /
     Bugfixes only; see the feedback memory).
   - Open as a small PR to `develop` so the changelog wording gets one round of
     review before it ships.
2. **Cut release**: after the prep PR merges, locally
   `git checkout main && git merge --no-ff develop && git push origin main`,
   then `git tag X.Y.Z` on the merge commit and `git push origin X.Y.Z`.
3. **GitHub release**: `gh release create X.Y.Z --title "X.Y.Z" --notes "..."`
   pasting the matching `CHANGELOG.md` section as the notes — the `/whats-new`
   page's `[X.Y.Z](.../releases/tag/X.Y.Z)` link expects a real release page,
   not a bare tag.
- **GitHub Pages deployment** is handled by `.github/workflows/github-pages.yml`,
  which auto-deploys on push to `main`. It uses the modern Pages-from-Actions
  artifact pattern (`actions/configure-pages` + `upload-pages-artifact` +
  `deploy-pages`) — no `gh-pages` branch involved. The repo's Pages source must
  be set to "GitHub Actions" in Settings → Pages for the workflow to publish.
- CI build/test (`.github/workflows/dotnet.yml`) runs only on push/PR to `main`.
  PRs to `develop` get the Claude auto-review but not the .NET build — verify
  locally before opening develop-targeted PRs.
