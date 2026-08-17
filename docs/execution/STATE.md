# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 7 — Planned Work → Service Record
Current task:       PHASE-07-01 (see docs/execution/PHASE_07.md) — idempotency + full type coverage
Status:             Complete. Idempotency proven via curl (completed the same plan 3x, exactly 1
                     resulting record - repeated for both a Service-type and a Gas-type plan); user
                     confirmed no regression in the (low-risk, non-drag-drop) UI changes.
Last completed:      Phase 6 finished (6-stage pipeline + Actual Cost, see PHASE_06.md).
                     Phase 7: fixed the two concrete gaps Phase 1 found in the existing completion-
                     conversion workflow. Idempotency: captured the plan's prior Progress before
                     overwriting it, gated the conversion block on "transitioning TO Done", so a
                     replayed/double-clicked completion no longer creates a duplicate record.
                     ImportMode coverage: added GasRecord/TaxRecord conversion branches (previously
                     silent no-ops), extended the Type dropdown and API validation whitelist so
                     they're reachable through the UI. Investigated standing up the first automated
                     test project (explicitly flagged for this exact fix) - found the LiteDB path
                     is a working-directory-relative path, so test isolation is achievable without
                     touching production code, but the user chose to keep deferring the actual test
                     infrastructure work; findings preserved in DEFERRED.md for later.
Next task:           Open decision: what's next - Phase 8 (Government Data, mocked DVLA/DVSA
                     adapters) is the natural next roadmap phase. Ask the user before starting it or
                     anything else, per CLAUDE.md's phase-boundary rule. DEFERRED.md items (now
                     including the test-infrastructure investigation) stay parked per the user's
                     2026-08-17 decision.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow continues to work well; not every phase needs the same depth of
                        live review (Phase 7's UI changes were low-risk enough that a quick optional
                        look sufficed, unlike Phase 6's drag-and-drop).
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items - all deferred to a finishing-touches pass, not
                        forgotten.
Open decisions:      What to build next - pending user input (Phase 8 is the natural candidate).
Do not:              Start Phase 8 or beyond without the user's go-ahead, per CLAUDE.md's
                     phase-boundary rule. Do not re-litigate or re-surface items already tracked in
                     DEFERRED.md as if they were forgotten - they're intentionally parked, including
                     the now-well-scoped test-infrastructure work. Do not assume SQLite is available
                     anywhere in this codebase. Do not treat "Planned Work -> Service Record" as
                     needing further hardening - Phase 7 closed out FR-PLAN-03/04/05/06 completely;
                     it's done, not just "exists". Do not assume a fresh vehicle/user has any tabs
                     visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only; check/set
                     it first or use the API directly when testing a vehicle detail page. When
                     touching interactive markup, keep using the diff-verify-then-user-confirms
                     workflow, calibrating review depth to actual interactive risk (drag-and-drop
                     needs live testing; a dropdown addition following an existing pattern mostly
                     doesn't). When calling record-add API endpoints for testing, field names/casing
                     are inconsistent across export models and dates must match the server's locale
                     (dd/mm/yyyy here) - check the relevant *ExportModel class in Models/Shared/
                     ImportModel.cs first rather than guessing. Part is NOT vehicle-scoped (global
                     catalog) but PartPurchase IS (VehicleId, 0=shop-wide). PartPurchase.
                     QuantityRemaining must be set explicitly by the caller, never by ToPartPurchase().
                     PlanRecord.ActualCost is preferred over Cost (estimate) by the completion-
                     conversion logic when non-zero, for all 5 target record types now.
Last validation:     dotnet build (0 errors, 224 warnings, unchanged); idempotency verified via curl
                     for both Service and Gas target types (3x and 2x completion respectively, exactly
                     1 resulting record each); Tax-type completion verified producing correct fields;
                     Type dropdown HTML confirmed showing all 5 options; user-confirmed no regression
                     live in browser — 2026-08-17.
Last commit:         e15aa79 — "Phase 7: idempotent plan completion + full ImportMode coverage"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
