# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 2 — UI Design System
Current task:       PHASE-02-01 (see docs/execution/PHASE_02.md) — design token foundation
Status:             This increment complete; broader phase paused pending visual verification
Last completed:     docs/UI_SPEC.md written; design tokens (spacing/radius/shadow/motion) added to
                     wwwroot/css/site.css and applied to .card/.taskCard/.kiosk-card (values kept
                     identical - pure consolidation); new opt-in .status-badge/.ct-empty-state
                     primitives added (not yet used by any view); fixed a real dark-mode bug in
                     wwwroot/css/loader.css (.sloader/.loader were hardcoded white, invisible-on-white
                     in dark mode). Zero .cshtml files touched. Build verified 0 errors, app verified
                     serving the new CSS at runtime.
Next task:           Broader Phase 2 rollout is blocked on visual verification capability, not on
                     more analysis. Specifically: consolidating the triplicated tab-bar markup
                     (Vehicle/Index.cshtml renders its 13-tab nav 3x, wired to vehicle.js by element
                     ID) needs either a browser/screenshot tool or the user reviewing it locally
                     before attempting - restructuring that markup blind risks breaking navigation
                     app-wide. Ask the user how they want to proceed (their own local review loop,
                     or wait for a visual-verification tool) before continuing Phase 2's nav/shell
                     and per-screen badge/empty-state rollout.
Known blockers:      1. No browser/screenshot tool available in this environment - cannot visually
                        verify UI changes, only structural/HTTP correctness. This is the actual
                        blocker on further Phase 2 work, not a knowledge gap.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
Open decisions:      How to continue Phase 2 without visual verification - human-in-the-loop review
                     of localhost after each increment, vs. waiting for a visual-verification tool,
                     vs. continuing with more CSS-only/non-interactive increments only.
Do not:              Restructure the shell/navigation markup (the triplicated tab-bar rendering)
                     without either a way to visually/interactively verify the result, or explicit
                     user sign-off to proceed blind. Do not assume SQLite is available anywhere in
                     this codebase. Do not treat "Planned Work -> Service Record" as unbuilt - it
                     exists and needs hardening (Phase 7), not a new implementation.
Last validation:     dotnet build (0 errors, same 209 pre-existing nullable warnings) + dotnet run,
                     curl-verified / and both modified CSS files serve 200 with the new rules present
                     in the served output — 2026-08-17.
Last commit:         45b3858 — "Phase 2 (partial): UI design token foundation"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
