# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 3 — Garage / Dashboard
Current task:       PHASE-03-01 (see docs/execution/PHASE_03.md) — active-projects dashboard metric
Status:             Complete and user-verified live in browser. Phase 3's buildable scope is now
                     largely done (see "Next task" for what's left/blocked).
Last completed:      Phase 2 finished (token foundation + shell/nav consolidation, user-verified,
                     see git history 3f63b5b..80a2b48 for detail if needed).
                     Phase 3: added DashboardMetric.ActiveProjects, a fourth opt-in Garage-card
                     metric (mirrors existing Default/CostPerMile/TotalCost pattern exactly) showing
                     a count of non-Done PlanRecords per vehicle. Verified end-to-end via API against
                     throwaway vehicles first, then user-confirmed live against their real vehicle.
                     Along the way, investigated what looked like a serious nav bug (vehicle page
                     only showing 2 of 14 tabs) - turned out to be default VisibleTabs config
                     ([Dashboard] only on fresh install), not a bug. Found and documented (but did
                     NOT fix/commit, since unverified) a real candidate bug in shared.js's
                     checkNavBarOverflow() - see PHASE_03.md "Investigation detour" section.
Next task:           Open decision: move to Phase 4 (Vehicle Experience) per ROADMAP.md, or address
                     something left over from Phase 3 first - candidates: (a) the shared.js
                     checkNavBarOverflow() candidate bug (needs a real overflow scenario to verify
                     against - enable several VisibleTabs + narrow browser), (b) richer
                     upcoming-work/reminder summarization on the dashboard beyond the existing binary
                     bell icon (noted as deferred in PHASE_03.md, larger scope). MOT status stays
                     blocked on Phase 8 regardless. Ask the user before starting Phase 4, per
                     CLAUDE.md's phase-boundary rule.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow with the user (implement + diff/API-verify, they confirm live)
                        is working well and should continue for any further interactive-markup work.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
                     4. Candidate bug in shared.js checkNavBarOverflow() (see PHASE_03.md) - found,
                        not fixed, not verified. Not blocking anything currently since it only
                        manifests under genuine tab overflow, which requires non-default VisibleTabs.
Open decisions:      Move to Phase 4, or clean up Phase 3 leftovers first - pending user input.
Do not:              Start Phase 4 (or any further phase) without the user's go-ahead, per
                     CLAUDE.md's phase-boundary rule. Do not assume SQLite is available anywhere in
                     this codebase. Do not treat "Planned Work -> Service Record" as unbuilt - it
                     exists and needs hardening (Phase 7). Do not assume a fresh vehicle/user has any
                     tabs visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only; if
                     testing anything on a vehicle detail page, check/set VisibleTabs first or use
                     the API directly to avoid re-discovering this "vehicle nav is broken" false
                     alarm. When touching interactive markup, keep using the diff-verify-then-user-
                     confirms workflow.
Last validation:     dotnet build (0 errors, 209 pre-existing nullable warnings, unchanged); Active
                     Projects feature verified end-to-end via API (badge shows/hides correctly per
                     opt-in and count) and confirmed live in the user's browser — 2026-08-17.
Last commit:         66958a7 — "Phase 3: active-projects dashboard metric on Garage cards"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
