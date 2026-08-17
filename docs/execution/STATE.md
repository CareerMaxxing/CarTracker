# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 4 — Vehicle Experience
Current task:       PHASE-04-01 (see docs/execution/PHASE_04.md) — browsable Documents tab
Status:             Complete and user-verified live in browser.
Last completed:      Phase 3 finished (active-projects dashboard metric, see PHASE_03.md).
                     Phase 4: checked all six target areas (Overview/Maintenance/History/Parts/
                     Projects/Documents) against what exists - five already had a home in the
                     current 13-tab structure. User chose to keep that structure rather than regroup
                     nav into the six categories. Built the one real gap: a browsable "Documents"
                     tab (14th tab, always visible) reusing the existing cross-record attachment
                     aggregation that previously only powered a zip-export button. Verified via API
                     against throwaway vehicles (attachment renders correctly, empty state works,
                     export button unaffected by the refactor), then user-confirmed live.
Next task:           Open decision: move to Phase 5 (Parts Domain - split SupplyRecord into a Part
                     catalog + Purchase entity per REQUIREMENTS.md FR-PART-01/02/03) per ROADMAP.md,
                     or address a leftover first - candidates: (a) the shared.js
                     checkNavBarOverflow() candidate bug from Phase 3 (still unverified, needs a
                     real overflow scenario - enable several VisibleTabs + narrow browser), (b)
                     richer upcoming-work/reminder summarization on the dashboard (deferred in
                     PHASE_03.md). Ask the user before starting Phase 5, per CLAUDE.md's
                     phase-boundary rule.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow with the user (implement + diff/API-verify, they confirm live)
                        is working well and should continue for any further interactive-markup work.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
                     4. Candidate bug in shared.js checkNavBarOverflow() (see PHASE_03.md) - found,
                        not fixed, not verified. Not blocking anything currently since it only
                        manifests under genuine tab overflow, which requires non-default VisibleTabs.
Open decisions:      Move to Phase 5, or clean up leftovers first - pending user input.
Do not:              Start Phase 5 (or any further phase) without the user's go-ahead, per
                     CLAUDE.md's phase-boundary rule. Do not assume SQLite is available anywhere in
                     this codebase. Do not treat "Planned Work -> Service Record" as unbuilt - it
                     exists and needs hardening (Phase 7). Do not assume a fresh vehicle/user has any
                     tabs visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only; if
                     testing anything on a vehicle detail page, check/set VisibleTabs first or use
                     the API directly to avoid re-discovering this "vehicle nav is broken" false
                     alarm. When touching interactive markup, keep using the diff-verify-then-user-
                     confirms workflow. When calling record-add API endpoints for testing, field
                     names/casing are inconsistent across export models (e.g. servicerecords/add
                     wants "odometer" not "mileage") - check the relevant *ExportModel class in
                     Models/Shared/ImportModel.cs first rather than guessing.
Last validation:     dotnet build (0 errors, 209 pre-existing nullable warnings, unchanged);
                     Documents tab verified end-to-end via API (attachment renders, empty state
                     works, export regression-checked) and confirmed live in the user's browser —
                     2026-08-17.
Last commit:         (pending — Phase 4 commit, created immediately after this file)
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
