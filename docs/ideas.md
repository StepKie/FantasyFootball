# Idea pile

Unsorted feature/UX ideas that aren't yet committed to a PR plan. Promote to
`ui-polish-plan.md` (or a dedicated planning doc) when an idea graduates to
"we're doing this next".

## Simulation model

- **Pluggable simulation models.** Today's sim is one Elo-based model with
  fixed parameters. Offer alternatives:
  - **Elo (current)** — baseline.
  - **Elo + outliers** — modified Elo with heavier tails (more upsets / blowouts).
  - **Tweakable parameters** — expose math knobs (Elo K-factor, goal-distribution
    variance, home-field advantage, draw bias) for an "Advanced" mode.
- UX shape: a `SimulationModel` setting (radio + per-model param panel) for
  power users. Default stays Elo with current params.

## Settings as overlay (not a separate page)

- Replace `/settings` page with a modal / dropdown / slide-over overlay
  triggered from the app bar.
- The page underneath stays visible → user sees the effect of a setting
  (flag style, game-row detail, sim speed) directly on the current page
  without navigating away and back.
- Mirrors how OneFootball / Sofascore handle preferences.

## Manual competition setup — drawing-ceremony UX

Applies to the *manual* setup mode only (Classic / Random unchanged):

- **Drag-and-drop** team entries between group slots.
- **Double-tap** an empty slot to type with autocomplete (existing team
  search).
- **Pot-based animated draw**: emulate the official ceremony — teams sit in
  pots, click "draw" to animate one team at a time landing in the next
  legal group slot. Subtle confetti/ball-bounce; respects standard draw
  constraints (no two teams from same confederation in one group, etc.).
- Natural extension of the existing manual setup; differentiates the app
  vs "just pick teams from a dropdown".
