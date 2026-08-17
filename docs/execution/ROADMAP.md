# ROADMAP

Ordered phases for the Car Tracker project. Each phase gets its own `PHASE_NN.md` with the task
packets for that phase once it starts. Do not start a phase early — see `CLAUDE.md`.

| # | Phase | Objective | Status |
|---|-------|-----------|--------|
| 0 | Baseline Reconnaissance | Establish the clean LubeLogger baseline: confirm it builds/runs, map architecture/data model/API/UI, no product changes. | ✅ Complete |
| 1 | Requirements & Architecture Reconciliation | Compare discovered LubeLogger capabilities against target Car Tracker requirements; produce EXISTING/MODIFY/EXTEND/REPLACE/NEW matrix; write SYSTEM_SPEC.md, REQUIREMENTS.md, update ARCHITECTURE.md/DATA_MODEL.md. | ✅ Complete |
| 2 | UI Design System | App shell, nav, typography/spacing, cards/buttons/forms/tables/dialogs, status badges, loading/empty/error states, responsive layout, theming — without touching the domain model. | Not started |
| 3 | Garage / Dashboard | "What's happening with my cars?" — vehicle overview cards, mileage, MOT status, upcoming work, reminders, active projects, cost summaries. | Not started |
| 4 | Vehicle Experience | Vehicle Overview / Maintenance / History / Parts / Projects / Documents, reusing existing data structures where possible. | Not started |
| 5 | Parts Domain | Parts as first-class entities: part number, manufacturer, description, category, fitment, purchases, suppliers, price history, documents. Price belongs to a purchase, not the part. | Not started |
| 6 | Planned Engineering Work | Idea → Costed → Parts Sourced → In Progress → Done pipeline for planned work per vehicle. | Not started |
| 7 | Planned Work → Service Record | Critical workflow: completing planned work idempotently creates a permanent service record, preserving parts/prices/mileage/notes/attachments. Dedicated acceptance tests required. | Not started |
| 8 | Government Data | Mocked DVLA/DVSA adapters behind a domain-facing interface; no real credentials in this phase. | Not started |
| 9 | Mileage / Odometer | One coherent odometer history across manual/service/MOT sources; flag suspicious regressions rather than silently accepting bad data. | Not started |
| 10 | Documents | First-class attachments (invoice, MOT, V5C, insurance, photo, datasheet, other) associated with vehicles, parts, planned work, and service records. | Not started |
| 11 | Global Search | Command-palette style search across parts, service records, projects, documents, vehicles. Confirm the right indexing approach given the actual data layer (see ARCHITECTURE.md — LiteDB/Postgres, not SQLite). | Not started |
| 12 | Local Reliability / Offline Hardening | Reliable startup, DB integrity, attachment integrity, backup, restore, export/import, recovery from interrupted operations. | Not started |
| 13 | AI/OCR | Explicitly deferred. Leave clean extension points only; no feature work. | Deferred |
| 14 | V1 Hardening | Unit/integration/UI/migration/duplicate-operation/adapter-failure/backup-restore tests, error handling, accessibility, responsive validation, performance, security review. | Not started |

## Human review checkpoints

- After Phase 0: review actual LubeLogger architecture.
- After Phase 1: approve the reconciled system specification and architecture. **← we are here, pending human review**
- After UI foundation (Phase 2): visually inspect the new design direction.
- After Parts + Planned Work (Phases 5–6): verify the core domain model.
- After Planned Work → Service Record (Phase 7): end-to-end acceptance test.
- Before real DVLA/DVSA adapters: approve credential/API architecture.
- Before V1: final system review.

## Core V1 acceptance scenario

```
ADD VEHICLE → IDENTIFY VEHICLE → LOAD/VIEW GOVERNMENT DATA (mocked) → VIEW MILEAGE HISTORY
→ ADD PARTS → CREATE PLANNED ENGINEERING WORK → COST THE WORK → ASSOCIATE/SOURCE PARTS
→ PERFORM WORK → COMPLETE WORK → AUTOMATICALLY CREATE SERVICE RECORD
→ PRESERVE PARTS + PURCHASE PRICES + MILEAGE + NOTES → ATTACH PHOTOS/INVOICE
→ SEARCH AND RETRIEVE COMPLETE HISTORY
```

This end-to-end flow is the practical definition of the system's core value; every phase above
exists to make it reliable, fast, and pleasant to use.
