# PHASE_01 — Requirements & Architecture Reconciliation

## Task packet

```
TASK ID: PHASE-01
TITLE: Requirements & Architecture Reconciliation
OBJECTIVE: Compare the discovered LubeLogger baseline (Phase 0) against Car Tracker's target
  requirements; produce a traceable EXISTING/MODIFY/EXTEND/REPLACE/NEW matrix; resolve
  architecture decisions from evidence rather than assumptions.
INPUTS: docs/SYSTEM_BASELINE.md, docs/ARCHITECTURE.md, docs/DATA_MODEL.md, docs/API_MAP.md,
  docs/UI_INVENTORY.md (all Phase 0), the original engineering specification's target capability
  list and phase descriptions.
ALLOWED SCOPE: Reading/analyzing existing code to verify specific capability claims (read-only);
  writing documentation. No product code changes.
NON-SCOPE: Any implementation of Parts, Planned Work changes, Government adapters, UI changes, or
  any other product feature. No domain model or controller code was touched.
IMPLEMENTATION REQUIREMENTS:
  - Produce a traceability matrix (EXISTING | MODIFY | EXTEND | REPLACE | NEW) covering every
    target capability area.
  - Verify uncertain classifications against the actual code rather than assuming (done: confirmed
    the Planned Work -> Service Record conversion already exists, with evidence).
  - Produce/update SYSTEM_SPEC.md, REQUIREMENTS.md, and update ARCHITECTURE.md/DATA_MODEL.md with
    target-state notes.
DELIVERABLES: docs/SYSTEM_SPEC.md, docs/REQUIREMENTS.md, target-state sections appended to
  docs/ARCHITECTURE.md and docs/DATA_MODEL.md, this file, updated docs/execution/STATE.md and
  docs/execution/ROADMAP.md.
ACCEPTANCE CRITERIA:
  - [x] Every target capability from the original spec's capability tree is classified with
        evidence (file/line references where the classification depends on specific code).
  - [x] No classification is asserted without either a Phase 0 doc citation or a fresh code check.
  - [x] SYSTEM_SPEC.md and REQUIREMENTS.md exist and are populated with concrete content, not
        placeholders.
  - [x] ARCHITECTURE.md and DATA_MODEL.md updated with target-state implications (not rewritten —
        the baseline sections from Phase 0 remain accurate and unchanged).
  - [x] No product functionality changed.
  - [x] Results committed to git.
VALIDATION COMMANDS: none (documentation-only phase; no build/run change expected — verified the
  repo still builds clean after these changes as a sanity check).
STOP CONDITION: Reconciliation complete, matrix + SYSTEM_SPEC.md + REQUIREMENTS.md exist, no
  product functionality changed, results committed -> stop and report for human review (per
  ROADMAP.md checkpoint "After Phase 1: approve the reconciled system specification and
  architecture").
```

## What was done

1. Re-read the Phase 0 baseline docs and the original engineering specification's target
   capability list (§3, §15–§26 phase descriptions) to build the reconciliation matrix.
2. Spot-verified one specific uncertain classification directly against the code rather than
   guessing: whether "Planned Work → Service Record" (the spec's flagged "critical differentiating
   workflow") already exists. It does —
   `Controllers/Vehicle/PlanController.cs:277-378` (`UpdatePlanRecordProgress`) already converts a
   completed `PlanRecord` into a `ServiceRecord`/`CollisionRecord`/`UpgradeRecord`. Also found two
   concrete gaps in that same code: it is **not idempotent** (no guard against re-running the
   conversion on repeated `Done` submissions) and **only handles 3 of the 5 `ImportMode` values** a
   plan can target (`GasRecord`/`TaxRecord` silently no-op). This reclassified the capability from
   NEW to MODIFY and directly shaped Phase 7's requirements (FR-PLAN-03 through FR-PLAN-06).
3. Wrote `docs/SYSTEM_SPEC.md` (target capability tree + guiding principles + what changed since
   the original spec was written).
4. Wrote `docs/REQUIREMENTS.md` (full traceability matrix + per-capability functional/non-functional
   requirements with evidence and acceptance criteria).
5. Appended target-state reconciliation sections to `docs/ARCHITECTURE.md` and `docs/DATA_MODEL.md`
   (did not rewrite the Phase 0 baseline content, which remains accurate).
6. Verified `dotnet build` still succeeds (0 errors) as a sanity check — no product code was
   touched, so this was expected to be unaffected.

## Key outcomes affecting later phases

- **Phase 7 scope changed**: from building a new workflow to hardening an existing one — add an
  idempotency guard and complete the `GasRecord`/`TaxRecord` branches in
  `PlanController.UpdatePlanRecordProgress`. This is very likely a smaller, more contained task
  than originally scoped, and a natural place to stand up the project's first automated test
  (idempotency is easy to regression-test and easy to silently break).
- **Phase 6 scope clarified**: `PlanProgress` enum needs new stages (`Idea`/`Costed`/
  `PartsSourced`/`InProgress`/`Done` replacing `Backlog`/`InProgress`/`Testing`/`Done`) and an
  `ActualCost` field distinct from the existing estimated `Cost`.
- **Phase 5 scope clarified**: split `SupplyRecord` into a `Part` catalog entity and a
  `PartPurchase`/`PartTransaction` entity; keep `SupplyUsage`/`SupplyUsageHistory` as-is.
- **Phase 9/10 scope clarified**: `OdometerRecord` needs a `Source` field; `UploadedFiles` needs a
  document-type field. Both are additive, no backfill/migration required since there's no
  pre-existing data (per locked decision).
- **Phase 8 confirmed straightforward**: the existing LiteDB/Postgres interface-segregation pattern
  in `External/` is directly reusable for DVLA/DVSA adapters — no new architectural pattern needed.
- **Phase 11 (search) intentionally left an open engineering decision**, not resolved here — see
  `REQUIREMENTS.md` FR-SEARCH-01. This isn't a stop condition (it's an implementation choice, not a
  product requirement conflict), just not yet decided.
- No REPLACE-classified requirements were identified — nothing in Phase 0's findings conflicts
  seriously enough with the target spec to warrant discarding working functionality.

## Result

Phase 1 complete per the acceptance criteria above. Per `ROADMAP.md`, the next step is the human
review checkpoint ("After Phase 1: approve the reconciled system specification and architecture")
before Phase 2 (UI Design System) begins.
