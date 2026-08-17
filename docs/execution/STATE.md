# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 2 — UI Design System
Current task:       PHASE-02 (see docs/execution/PHASE_02.md) — foundation + shell/nav consolidation
Status:             Foundation and shell/nav consolidation complete and user-verified; remaining
                     Phase 2 scope is smaller/lower-risk, open question on whether to continue here
                     or move to Phase 3 (see "Next task" below)
Last completed:      1. Design tokens (spacing/radius/shadow/motion) in site.css, applied to
                     .card/.taskCard/.kiosk-card (values kept identical); new opt-in
                     .status-badge/.ct-empty-state primitives (not yet used by any view); fixed a
                     real dark-mode bug in loader.css (hardcoded white overlay/spinner).
                     2. Workflow decision resolved: user chose "review locally as I go" - I
                     implement + diff-verify an increment, they check it live in a browser before I
                     commit.
                     3. Consolidated the triplicated tab-bar markup in Home/Index.cshtml (4 tabs)
                     and Vehicle/Index.cshtml (13 tabs x3 renderings) into shared tab-definition
                     lists. Both verified by diffing actual rendered HTML before/after (caught and
                     fixed one real bug: stray whitespace in mobile nav labels from a Razor quirk)
                     AND confirmed working live in the user's browser (overflow dropdown, mobile
                     off-canvas nav, tab switching, dark mode).
                     4. Audited the remaining 4 @section Nav-bearing views (Migration/Admin/API/
                     Setup) - none have the triplication pattern, so this specific structural issue
                     (flagged in Phase 0's UI_INVENTORY.md) is now fully resolved codebase-wide.
Next task:           Open decision: continue Phase 2 (typography scale, forms/tables/dialogs
                     styling, adopt .status-badge/.ct-empty-state into a real screen, or the smaller
                     Setup-wizard desktop/mobile nav duplication) vs. call the Phase 2 foundation
                     "done enough" (major workflows intact, using the new token foundation, the
                     flagged structural issue resolved) and move to Phase 3 (Garage/Dashboard) per
                     ROADMAP.md - ask the user which they'd prefer before proceeding, since starting
                     Phase 3 early without a nod would violate CLAUDE.md.
Known blockers:      1. Still no browser/screenshot tool in this environment - the "review locally
                        as I go" workflow with the user is what unblocks any further interactive-
                        markup work, and is working well.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
Open decisions:      Continue polishing Phase 2, or move to Phase 3 - pending user input.
Do not:              Start Phase 3 (or any further phase) without the user's go-ahead, per
                     CLAUDE.md's phase-boundary rule. Do not assume SQLite is available anywhere in
                     this codebase. Do not treat "Planned Work -> Service Record" as unbuilt - it
                     exists and needs hardening (Phase 7), not a new implementation. When touching
                     any further interactive markup, keep using the diff-verify-then-user-confirms
                     workflow that worked well in this phase.
Last validation:     dotnet build (0 errors, 209 pre-existing nullable warnings, unchanged) after
                     every increment; rendered-HTML diffs against captured baselines for both nav
                     consolidations; user-confirmed live in browser for both — 2026-08-17.
Last commit:         b3016fa — "Phase 2: deduplicate Vehicle detail page nav markup"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
