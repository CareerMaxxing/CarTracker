# SYSTEM_SPEC.md

What Car Tracker is intended to be, reconciled against the verified LubeLogger baseline
(`docs/SYSTEM_BASELINE.md`). This is the target-state counterpart to that baseline document —
where they conflict, the locked decisions in `CLAUDE.md` win.

## Purpose

Car Tracker is a personal, local-first vehicle engineering and maintenance management system. It
maintains authoritative vehicle history, understands parts and fitment, manages planned engineering
work, and converts completed planned work into permanent service history — built on top of
LubeLogger's existing, working vehicle-maintenance domain rather than rebuilt from scratch.

## Target capability tree

```
CAR TRACKER
├── Vehicles                                  — EXISTING (LubeLogger Vehicle entity)
├── Maintenance / Service History              — EXISTING (Service/Repair/Upgrade/Tax/Gas records)
├── Parts                                      — EXTEND (SupplyRecord → Part + Purchase split)
├── Planned Engineering Work                   — EXTEND (PlanRecord already close to this)
├── Documents / Attachments                    — EXTEND (UploadedFiles + typed categorization)
├── Mileage / Odometer History                 — EXTEND (OdometerRecord + Source + regression flags)
├── Reminders / Upcoming Work                  — EXISTING (ReminderRecord)
├── Government Data (DVLA/DVSA adapters)       — NEW (mocked adapters, real domain has nothing today)
└── Search / Dashboard / UI                    — EXTEND (Garage dashboard exists; global search doesn't)
```

Full evidence and reasoning for each classification is in `docs/REQUIREMENTS.md`'s traceability
matrix.

## Guiding principles (from the locked spec, restated here for reference)

- Treat LubeLogger as the baseline system, not the final product — reverse-engineer before
  changing architecture (done in Phase 0).
- Preserve working functionality until its replacement has parity and acceptance tests.
- Prefer incremental, reversible changes over rewrites.
- Keep authoritative external data (government/DVLA/DVSA) read-only in the domain model.
- Use adapters for external integrations so mocked services can be swapped for real ones without
  rewriting the domain — LubeLogger already does exactly this for its storage backend
  (`External/Interfaces` + two `External/Implementations`), so government-data adapters should
  follow the same pattern.
- Keep the application local and simple until a real requirement justifies more infrastructure.

## Locked decisions (unchanged from Phase 0 — see CLAUDE.md for the authoritative copy)

Private GitHub repo · local/localhost runtime only · no pre-existing data to migrate · responsive
web now, native mobile deferred · local-first, no cloud sync · AI/OCR deferred out of V1 · mocked
DVLA/DVSA only until explicit sign-off on real credentials · build on LubeLogger · major UI overhaul
with functional parity preserved through the transition.

## What changed since the original spec was written

Two corrections, both discovered with evidence during Phase 0, both now binding:

1. **Persistence is LiteDB/PostgreSQL, not SQLite.** The original spec's Phase 11 (Global Search)
   assumption ("prefer the existing SQLite FTS5 direction") does not hold. See `ARCHITECTURE.md`
   target-state notes for the revised direction.
2. **"Planned Work → Service Record" already exists in LubeLogger**, not as a gap to fill from
   scratch. `PlanController.UpdatePlanRecordProgress` already converts a completed `PlanRecord`
   into a `ServiceRecord`/`CollisionRecord`/`UpgradeRecord`, preserving cost, notes, files, parts
   requisition history, and pushing back linked reminders. It is, however, **not idempotent** (no
   guard against re-running the conversion) and **only covers 3 of the 5 record types** a plan can
   target (`GasRecord` and `TaxRecord` are silently no-ops). This changes Phase 7 from "build this
   workflow" to "harden and complete this workflow" — see `REQUIREMENTS.md` FR-PLAN-04/05.

## Non-goals for V1 (explicitly out of scope, do not build)

AI/OCR of any kind · real DVLA/DVSA credentials/integration · cloud sync or PocketBase-style
architecture · native mobile app · destructive migration tooling for pre-existing user data (there
is none to migrate) · anything not traceable to a requirement in `REQUIREMENTS.md`.
