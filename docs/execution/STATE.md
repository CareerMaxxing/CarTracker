# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 6 — Planned Engineering Work
Current task:       PHASE-06-01 (see docs/execution/PHASE_06.md) — 6-stage pipeline + Actual Cost
Status:             Complete. Verified extensively via curl (all 6 stages, transitions, the
                     ActualCost-preferred/Cost-fallback completion logic, Active Projects count),
                     then user-confirmed live in browser including drag-and-drop and the context
                     menu (the parts curl categorically can't test).
Last completed:      Phase 5 finished (Part/PartPurchase domain, see PHASE_05.md).
                     Phase 6: expanded PlanProgress from 4 stages (Backlog/InProgress/Testing/Done)
                     to 6 (Idea/Costed/PartsSourced/InProgress/Testing/Done) - renamed Backlog->Idea
                     (same underlying value, zero migration risk), added Costed/PartsSourced as new
                     values. User chose to keep Testing (not in the original 5-stage target) rather
                     than remove working functionality. Added ActualCost alongside the existing Cost
                     (now "estimated"), and wired it into the existing completion-conversion logic
                     (Phase 1 finding) to be preferred over the estimate when set - verified both the
                     ActualCost and Cost-fallback paths explicitly. Swept all 13 files referencing
                     PlanProgress found via grep before touching anything, to catch the Kiosk view's
                     parallel implementation and the API-exposed stage-count field ahead of time
                     rather than discovering them broken later.
Next task:           Open decision: what's next - candidates are Phase 7 (Planned Work -> Service
                     Record, likely small given FR-PLAN-03/04/05 already scoped it down to an
                     idempotency fix + 2 missing branches in existing code), Phase 8 (Government
                     Data, mocked adapters), or continuing further into the roadmap. Per the user's
                     2026-08-17 decision, DEFERRED.md items stay parked for a finishing-touches pass,
                     not picked up mid-stream. Ask the user before starting Phase 7 or beyond, per
                     CLAUDE.md's phase-boundary rule.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow with the user continues to work well, including for genuinely
                        interactive features (drag-and-drop) that curl can't simulate at all.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items - all deferred to a finishing-touches pass, not
                        forgotten.
Open decisions:      What to build next - pending user input (see "Next task" candidates above).
Do not:              Start Phase 7 or beyond without the user's go-ahead, per CLAUDE.md's
                     phase-boundary rule. Do not re-litigate or re-surface items already tracked in
                     DEFERRED.md as if they were forgotten - they're intentionally parked. Do not
                     assume SQLite is available anywhere in this codebase. Do not treat "Planned
                     Work -> Service Record" as unbuilt - it exists (Controllers/Vehicle/
                     PlanController.cs UpdatePlanRecordProgress) and needs hardening (Phase 7:
                     idempotency + GasRecord/TaxRecord branches), not a new implementation. Do not
                     assume a fresh vehicle/user has any tabs visible beyond Dashboard - VisibleTabs
                     defaults to [Dashboard] only; if testing anything on a vehicle detail page,
                     check/set VisibleTabs first or use the API directly. When touching interactive
                     markup, keep using the diff-verify-then-user-confirms workflow - and for
                     genuinely interactive features (drag-and-drop, context menus), be explicit with
                     the user about what curl verified vs. what only they can check. When calling
                     record-add API endpoints for testing, field names/casing are inconsistent across
                     export models (e.g. servicerecords/add wants "odometer" not "mileage"; dates
                     must match the server's locale format, dd/mm/yyyy here, not US-style) - check
                     the relevant *ExportModel class in Models/Shared/ImportModel.cs first rather
                     than guessing. Part is NOT vehicle-scoped (global catalog) but PartPurchase IS
                     (VehicleId, 0=shop-wide) - don't conflate the two. PartPurchase.QuantityRemaining
                     must be set explicitly by the caller, never by ToPartPurchase(). PlanRecord.
                     ActualCost is optional/manually-entered and preferred over Cost (estimate) by
                     the completion-conversion logic when non-zero - see the comment in
                     Controllers/Vehicle/PlanController.cs's UpdatePlanRecordProgress.
Last validation:     dotnet build (0 errors, 224 warnings, unchanged from Phase 5 - no new
                     warnings); all 6 PlanProgress stages + transitions + ActualCost-preferred/
                     Cost-fallback completion logic verified via curl against a throwaway vehicle;
                     Kanban board HTML confirmed correct swimlane order/counts; add/edit modal HTML
                     confirmed correct dropdown options and cost fields; Phase 3's Active Projects
                     count reverified correct across the expanded pipeline; user-confirmed live in
                     browser including drag-and-drop and the context menu — 2026-08-17.
Last commit:         d957545 — "Phase 6: expand Planner to 6-stage pipeline, add Actual Cost"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
