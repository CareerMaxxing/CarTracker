# PHASE_07 — Planned Work → Service Record

## Scope

Per Phase 1's reconciliation (`REQUIREMENTS.md` FR-PLAN-04/05/06), this workflow already exists
(`Controllers/Vehicle/PlanController.cs` `UpdatePlanRecordProgress`) and needed hardening, not a
new implementation:

- **FR-PLAN-04 (critical)**: the completion-to-service-record conversion must be idempotent.
  It wasn't - calling it twice with `Done` created a second duplicate record every time, since the
  conversion block never checked the plan's *prior* progress before re-running.
- **FR-PLAN-05**: completing a plan targeting Gas or Tax silently did nothing (only
  Service/Repair/Upgrade were handled) - it should either produce the record or fail loudly, never
  silently no-op.

## Task packet

```
TASK ID: PHASE-07-01
TITLE: Idempotent completion + complete ImportMode coverage
OBJECTIVE: Fix the two concrete gaps Phase 1 found in the existing planned-work-to-service-record
  conversion, without touching its otherwise-working behavior (odometer auto-insert, reminder
  pushback, event publishing, the ActualCost-preferred-over-estimate logic added in Phase 6).
INPUTS: Controllers/Vehicle/PlanController.cs (UpdatePlanRecordProgress), Controllers/API/
  PlanController.cs (Type validation whitelist), Views/Vehicle/Plan/{_PlanRecordModal,
  _PlanRecordTemplateEditModal,_PlanRecordItem}.cshtml, Models/GasRecord/GasRecord.cs,
  Models/TaxRecord/TaxRecord.cs (target record shapes).
ALLOWED SCOPE: The idempotency guard; two new conversion branches (GasRecord, TaxRecord) mirroring
  the existing three exactly; extending the "Type" dropdown and API validation whitelist to make
  the two new branches actually reachable; two new card badge icons for visual consistency.
NON-SCOPE: Any other behavior in UpdatePlanRecordProgress (untouched); standing up automated test
  infrastructure (explicitly deferred - see below); CSV import/export awareness of Gas/Tax as plan
  target types (not touched, narrower gap, not required for this task's acceptance criteria).
IMPLEMENTATION REQUIREMENTS:
  - Capture existingRecord.Progress (the prior value) before overwriting it with the incoming
    planProgress; only run the conversion block when transitioning TO Done, i.e. gate on
    "planProgress == Done && priorProgress != Done".
  - Add GasRecord and TaxRecord branches to the existing if/else-if chain, using the same field-
    copying pattern (Description, Cost-preferring-ActualCost, Notes, Files, ExtraFields,
    RequisitionHistory where the target type has it) as the three existing branches.
  - Extend the Type dropdown (both the live-record modal and the template modal) and the API's
    type-validation whitelist to include GasRecord/TaxRecord, so the fix is reachable through the
    UI, not just directly via the API.
  - Add matching card badge icons (fuel pump / currency icon) for visual consistency with the
    existing three record-type badges.
DELIVERABLES: Idempotent completion workflow covering all 5 target record types.
ACCEPTANCE CRITERIA:
  - Completing the same plan record 3 times in a row produces exactly 1 resulting record, verified
    for both a Service-type plan and a Gas-type plan.
  - Completing a Gas-targeted plan produces a GasRecord; completing a Tax-targeted plan produces a
    TaxRecord - both previously silent no-ops.
  - The Type dropdown in both plan modals offers all 5 types; the API accepts all 5 without the
    previous "Type can only ServiceRecord, RepairRecord, or UpgradeRecord" rejection.
  - No regression to the three previously-working conversion branches or to Phase 6's ActualCost-
    preference logic.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against a throwaway vehicle: add a Service-type plan, call
  UpdatePlanRecordProgress with Done three times in a row, confirm exactly 1 ServiceRecord exists;
  repeat for a Gas-type plan (also exercises idempotency for the new branch); add and complete a
  Tax-type plan, confirm exactly 1 TaxRecord with correct fields; fetch the add-plan-record modal
  HTML and confirm all 5 Type options render.
STOP CONDITION: Acceptance criteria met, verified via curl, user confirmed no visual regression in
  the (low-risk, non-drag-drop) UI changes, changes committed.
```

## What was done

1. Re-read the exact current state of `UpdatePlanRecordProgress` (Phase 6 had already modified the
   Cost-vs-ActualCost lines within it) before editing, to make the idempotency guard and new
   branches land correctly alongside that prior change rather than conflicting with it.
2. Added the idempotency guard: `bool wasAlreadyDone = existingRecord.Progress == PlanProgress.Done;`
   captured *before* `existingRecord.Progress` is overwritten, then gated the conversion block on
   `planProgress == PlanProgress.Done && !wasAlreadyDone`.
3. Added GasRecord and TaxRecord branches. Noted a genuine field-mapping gap for Gas specifically:
   `GasRecord.Gallons` has no equivalent on `PlanRecord` (defaults to 0 - the user can edit the
   resulting record afterward); `TaxRecord` maps cleanly (Description/Cost/Notes/Files all have
   direct equivalents, no RequisitionHistory field on TaxRecord so that's simply not copied).
4. Extended the Type dropdown (both modals) and the API's validation whitelist (both the generic
   "any field invalid" message and the specific "Type can only..." message, in both Add and Update
   actions) to include the 2 new types - without this, the new branches would only be reachable by
   direct API manipulation bypassing the UI's own dropdown constraint.
5. Verified via curl before presenting to the user: completed the same Service-type plan 3 times in
   a row (exactly 1 ServiceRecord resulted), completed a Gas-type plan twice in a row (exactly 1
   GasRecord - confirming the idempotency fix generalizes, not just patched for one branch),
   completed a Tax-type plan once (correct TaxRecord fields), confirmed the modal's Type dropdown
   renders all 5 options, confirmed `/health` stayed green and the user's real vehicle was
   untouched throughout.
6. Discovered, while investigating whether to stand up automated test infrastructure (see below),
   that `Helper/StaticHelper.cs`'s `DbName = "data/cartracker.db"` is a relative path resolved
   against the process's working directory (via `LiteDBHelper`'s `new LiteDatabase(StaticHelper.DbName)`)
   rather than app configuration - meaning test isolation is achievable by controlling the test
   process's working directory, without any production code changes. Documented this finding for
   whenever the test-infrastructure work actually happens (see below).

## Test infrastructure: investigated, deliberately deferred

`CLAUDE.md`'s own test-infrastructure note flagged this exact fix as "a strong candidate for
standing up the first real test." Investigated feasibility before starting: `WebApplicationFactory<Program>`-based
integration testing is viable (this codebase's fat controllers with 20+ constructor dependencies
make unit-testing individual actions impractical without extensive mocking, but a real HTTP-level
integration test sidesteps that entirely - which is effectively what the curl-based verification
above already does, just manually). Confirmed the LiteDB path issue above is solvable without
touching production code. Remaining known work, recorded here so a future increment can start
without re-deriving it:

- Add `public partial class Program { }` at the end of `Program.cs` (top-level statement programs
  generate this class `internal` by default; a test project in a separate assembly needs it public
  to reference as `WebApplicationFactory<Program>`'s type parameter).
- New test project (`CarCareTracker.Tests` or similar) referencing `Microsoft.AspNetCore.Mvc.Testing`
  + xUnit, added to `CarCareTracker.sln`.
- Test isolation: set the test process's working directory to a fresh temp folder before the
  `WebApplicationFactory` opens its first LiteDB connection, so `data/cartracker.db` resolves
  there instead of colliding with real project data.
- Parallel-test safety: xUnit parallelizes test classes by default: multiple test classes sharing
  one working-directory-relative DB path would interfere with each other. Needs either a shared
  fixture with proper lifecycle management, or `[Collection]`-based serialization, or a per-test
  temp directory strategy.

**Explicit user decision (2026-08-17)**: continue deferring this rather than dedicating a separate
increment to it now - added to `DEFERRED.md` with the above technical detail preserved.

## Result

Complete and user-confirmed. `REQUIREMENTS.md`'s FR-PLAN-03/04/05/06 (the whole Phase 7 acceptance
scenario) are now all satisfied by working, curl-verified code.
