# PHASE_06 — Planned Engineering Work

## Design decisions (confirmed with user before implementation)

The target pipeline is `Idea → Costed → Parts Sourced → In Progress → Done`, but the existing
`PlanRecord`/`PlanProgress` implementation already has a working 4-stage Kanban board
(`Backlog/InProgress/Testing/Done`) with drag-and-drop, a "Move To" context menu, CSV import/export,
Kiosk display, and — critically, discovered in Phase 1 — a working "complete plan → auto-create
service record" conversion. Two decisions were made before touching code:

1. **Keep the existing `Testing` stage** (not in the target list) rather than removing it — user's
   explicit choice, preserves working functionality per `CLAUDE.md`'s "don't delete before parity"
   principle. Final pipeline: `Idea → Costed → Parts Sourced → InProgress → Testing → Done` (6
   stages).
2. **Rename `Backlog` → `Idea`, don't renumber** (`Idea=0` keeps the same underlying int value
   `Backlog` had) — zero data-migration risk, existing/future serialized records read correctly
   unchanged. `Costed=4` and `PartsSourced=5` are new slots appended after the existing values, not
   inserted between them, so nothing already-serialized shifts meaning.

`ActualCost` was added as a new field alongside the existing `Cost` (now documented as "estimated"),
editable manually - not auto-computed from parts consumption (that's separately deferred, see
`DEFERRED.md`).

## Task packet

```
TASK ID: PHASE-06-01
TITLE: Expand PlanProgress pipeline to 6 stages + Actual Cost
OBJECTIVE: Add the two missing target pipeline stages (Costed, Parts Sourced) and an Actual Cost
  concept to the existing Planned Engineering Work feature, without breaking any of its existing
  functionality (drag-and-drop Kanban, context-menu stage moves, CSV import/export, Kiosk display,
  the completion-to-service-record conversion, or the Phase 3 Active Projects dashboard metric).
INPUTS: Enum/PlanProgress.cs, Models/PlanRecord/{PlanRecord,PlanRecordInput}.cs,
  Models/Shared/ImportModel.cs (PlanRecordExportModel), Controllers/API/PlanController.cs,
  Controllers/Vehicle/PlanController.cs (including the UpdatePlanRecordProgress completion-
  conversion logic found in Phase 1), Views/Vehicle/Plan/*.cshtml, Views/Kiosk/_KioskPlan*.cshtml,
  wwwroot/js/planrecord.js, plus every other file a grep for "PlanProgress" turned up
  (Controllers/Vehicle/InspectionController.cs, Controllers/Vehicle/ImportController.cs,
  Helper/StaticHelper.cs, Logic/VehicleLogic.cs).
ALLOWED SCOPE: PlanProgress enum; ActualCost field addition end-to-end (model, input, API export
  model, API controller, MVC controller, add/edit modal UI); Kanban board layout (2 new swimlanes,
  correctly ordered); drag-drop and context-menu JS for the 2 new stages; Kiosk display (folded
  into the existing "not started" bucket rather than adding 2 more Kiosk columns); making the
  completion-conversion prefer ActualCost over Cost when set.
NON-SCOPE: FR-PLAN-04 (idempotency) and FR-PLAN-05 (missing GasRecord/TaxRecord branches) in the
  completion-conversion logic - both explicitly Phase 7's job, orthogonal to this phase's stage/
  cost changes and not touched. CSV import/export column mapping for ActualCost (a narrower,
  lower-priority gap, noted below rather than fixed). Auto-computing ActualCost from Parts
  consumption (Parts consumption wiring itself is deferred, see DEFERRED.md).
IMPLEMENTATION REQUIREMENTS:
  - PlanProgress: rename Backlog->Idea (same value), add Costed=4, PartsSourced=5.
  - PlanRecord/PlanRecordInput: add ActualCost decimal, wired through ToPlanRecord().
  - PlanRecordExportModel: add ActualCost (string+FromDecimalOptional, matching existing convention).
  - API PlanController: Add/Update actions parse and persist ActualCost (optional - falls back to
    0 on add, preserves existing value on update if omitted); validation error messages updated to
    list the new stage names.
  - MVC PlanController: GetPlanRecordForEditById carries ActualCost into the edit modal;
    UpdatePlanRecordProgress's three completion branches (ServiceRecord/CollisionRecord/
    UpgradeRecord) use ActualCost when it's non-zero, otherwise fall back to Cost (unchanged
    behavior for anyone who doesn't use the new field).
  - Kanban board: 2 new swimlanes (desktop + mobile-nav + visible-columns checkboxes), correctly
    positioned Idea/Costed/PartsSourced/InProgress/Testing/Done; 2 new "Move To" context-menu
    entries; drag-drop (dropBox) and the touch/right-click context menu's switch statement handle
    all 6 stages now.
  - Add/edit modal: stage dropdown gains the 2 new options (still excluding Done, which stays
    reachable only via the drag/completion flow); new "Actual Cost(optional)" input alongside the
    renamed "Estimated Cost" label.
  - Kiosk: Idea/Costed/PartsSourced share one display column (kept simple, at-a-glance) rather than
    growing Kiosk to 6 columns; explicit code comment explaining why.
  - Swept every other PlanProgress reference found via grep (InspectionController's auto-generated
    plan, VehicleLogic's stage-count aggregation and Done-filter, StaticHelper's display-string
    switch, ImportController's CSV mapping and sample generator, the API explorer's example JSON)
    and updated or confirmed each one is auto-compatible (Enum.TryParse-based code needed no
    changes; hardcoded switches/cases did).
DELIVERABLES: Working 6-stage Kanban board with Actual Cost support.
ACCEPTANCE CRITERIA:
  - All 6 stages accept records via the API and render correctly on the Kanban board, in pipeline
    order, with correct per-column counts.
  - Moving a record between any two stages (simulating drag-drop via the same endpoint the UI
    calls) works.
  - Completing a plan with ActualCost set produces a resulting record using ActualCost, not the
    estimate; completing one without ActualCost set falls back to the estimate (regression check).
  - Phase 3's Active Projects dashboard count still correctly counts non-Done records regardless of
    which of the 6 stages they're in.
  - The add/edit modal renders the new stage options and the new Actual Cost field correctly.
  - No dotnet build errors; no new compiler warnings beyond the existing baseline.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against a throwaway vehicle: add records at Idea/Costed/PartsSourced,
  confirm Kanban HTML shows 6 correctly-ordered/counted swimlanes; move a record between stages;
  complete one plan with ActualCost set and one without, confirm the resulting ServiceRecord's cost
  in each case; check the Garage Active Projects badge; fetch the add/edit modal HTML and confirm
  the new dropdown options and cost field render.
STOP CONDITION: Acceptance criteria met, verified via curl, user confirmed live in browser
  (including drag-and-drop and the context menu, which curl can't simulate), changes committed.
```

## What was done

1. Confirmed two design decisions with the user before writing code (keep Testing; rename-not-
   renumber Backlog->Idea) rather than assuming either.
2. Grepped the whole codebase for `PlanProgress` first (13 files) to understand the full blast
   radius before touching anything - this surfaced things easy to miss otherwise: the Kiosk plan
   view has its own parallel 4-column implementation; `VehicleInfo`'s API-exposed stage counts
   would silently undercount once new stages existed; the add/edit modal's stage dropdown
   deliberately excludes "Done" (only reachable via the drag/completion flow, to force the
   odometer-entry step) so the 2 new options needed to go in without disturbing that exclusion.
3. Made the enum change first (rename + additive new values) and then propagated: domain model,
   input DTO, API export model, both controllers (API and MVC), the Kanban board (6 swimlanes in
   3 places - desktop, mobile-nav, visible-columns toggle - plus the context menu), the add/edit
   modal (2 modals - live record and template both had the same 3-option dropdown), Kiosk, and
   every helper/logic file the initial grep found.
4. Made a judgment call beyond the minimum ask: wired `ActualCost` into the existing completion-
   conversion logic (preferring it over the estimate when set) rather than leaving it as a field
   that's captured but never used - reasoned this was in-scope for Phase 6 itself (the original
   spec lists "Actual cost" and "Completion information" together as Phase 6 concepts) and
   deliberately did NOT touch the two adjacent things that are explicitly Phase 7's job
   (idempotency, missing GasRecord/TaxRecord branches) even though they're in the same function.
5. Verified extensively via curl before user involvement: all 6 stages, stage transitions, the
   ActualCost-preferred/Cost-fallback completion behavior (both branches explicitly tested), the
   Kanban board's rendered HTML (swimlane order, counts, data-column attributes), the add/edit
   modal's rendered HTML (dropdown options, new cost field), and Phase 3's Active Projects count
   continuing to work correctly across the expanded pipeline. Was explicit with the user about the
   one thing curl categorically can't test (drag-and-drop, the context menu) and asked them to
   check those specifically - they confirmed working.

## Deferred (documented, not forgotten - added to DEFERRED.md)

- **CSV import/export column mapping for ActualCost** - `PlanRecordExportModel` has the field and
  the API honors it, but the CSV sample generator and column mapping weren't extended, so
  round-tripping a plan record through CSV export/import would currently lose its ActualCost value.
- Everything already listed in `DEFERRED.md` from earlier phases remains open (Phase 7's
  idempotency/coverage fixes, Parts consumption wiring, etc.) - unaffected by this phase.

## Result

Complete and user-verified live in browser, including the drag-and-drop and context-menu
interactions that couldn't be checked via curl alone.
