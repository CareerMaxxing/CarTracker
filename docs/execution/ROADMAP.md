# ROADMAP

Ordered phases for the Car Tracker project. Each phase gets its own `PHASE_NN.md` with the task
packets for that phase once it starts. Do not start a phase early — see `CLAUDE.md`.

| # | Phase | Objective | Status |
|---|-------|-----------|--------|
| 0 | Baseline Reconnaissance | Establish the clean LubeLogger baseline: confirm it builds/runs, map architecture/data model/API/UI, no product changes. | ✅ Complete |
| 1 | Requirements & Architecture Reconciliation | Compare discovered LubeLogger capabilities against target Car Tracker requirements; produce EXISTING/MODIFY/EXTEND/REPLACE/NEW matrix; write SYSTEM_SPEC.md, REQUIREMENTS.md, update ARCHITECTURE.md/DATA_MODEL.md. | ✅ Complete |
| 2 | UI Design System | App shell, nav, typography/spacing, cards/buttons/forms/tables/dialogs, status badges, loading/empty/error states, responsive layout, theming — without touching the domain model. | 🟡 In progress — token foundation + shell/nav consolidation done and user-verified (PHASE_02.md); typography/forms/tables/badges rollout still open |
| 3 | Garage / Dashboard | "What's happening with my cars?" — vehicle overview cards, mileage, MOT status, upcoming work, reminders, active projects, cost summaries. | 🟡 Mostly done — active projects added & verified; MOT status blocked on Phase 8; richer upcoming-work summary deferred (PHASE_03.md) |
| 4 | Vehicle Experience | Vehicle Overview / Maintenance / History / Parts / Projects / Documents, reusing existing data structures where possible. | ✅ Complete — 5/6 areas already existed, added a browsable Documents tab for the 6th (PHASE_04.md) |
| 5 | Parts Domain | Parts as first-class entities: part number, manufacturer, description, category, fitment, purchases, suppliers, price history, documents. Price belongs to a purchase, not the part. | ✅ Complete — backend, API, and UI all done and user-verified (PHASE_05.md); consumption wiring/ImportMode integration/catalog browse screen deferred |
| 6 | Planned Engineering Work | Idea → Costed → Parts Sourced → In Progress → Done pipeline for planned work per vehicle. | ✅ Complete — 6-stage pipeline (kept Testing per user choice) + Actual Cost, user-verified including drag-and-drop (PHASE_06.md) |
| 7 | Planned Work → Service Record | Critical workflow: completing planned work idempotently creates a permanent service record, preserving parts/prices/mileage/notes/attachments. Dedicated acceptance tests required. | ✅ Complete — idempotency fixed + Gas/Tax coverage added, curl-verified (PHASE_07.md); automated test project explicitly deferred to DEFERRED.md |
| 8 | Government Data | Mocked DVLA/DVSA adapters behind a domain-facing interface; no real credentials in this phase. | ✅ Complete — deterministic mock adapters + API endpoint + Dashboard panel, curl-verified (PHASE_08.md); MOT-status Garage badge left for a later increment |
| 9 | Mileage / Odometer | One coherent odometer history across manual/service/MOT sources; flag suspicious regressions rather than silently accepting bad data. | ✅ Complete — Source provenance on every reading + non-blocking regression warning on manual entry, curl-verified (PHASE_09.md); regression-check on the other auto-insert forms and CSV Source column left as candidates |
| 10 | Documents | First-class attachments (invoice, MOT, V5C, insurance, photo, datasheet, other) associated with vehicles, parts, planned work, and service records. | ✅ Complete — DocumentType categorization + Documents tab filtering, curl-verified (PHASE_10.md); a systemic bug where Type was silently dropped on every MVC save was caught and fixed before shipping |
| 11 | Global Search | Command-palette style search across parts, service records, projects, documents, vehicles. Confirm the right indexing approach given the actual data layer (see ARCHITECTURE.md — LiteDB/Postgres, not SQLite). | ✅ Complete — cross-vehicle search extending existing in-app filtering (decision recorded in ARCHITECTURE.md), curl-verified (PHASE_11.md); deep-linking to a specific record across a vehicle navigation left as a candidate refinement |
| 12 | Local Reliability / Offline Hardening | Reliable startup, DB integrity, attachment integrity, backup, restore, export/import, recovery from interrupted operations. | Not started |
| 13 | AI/OCR | Explicitly deferred. Leave clean extension points only; no feature work. | Deferred |
| 14 | V1 Hardening | Unit/integration/UI/migration/duplicate-operation/adapter-failure/backup-restore tests, error handling, accessibility, responsive validation, performance, security review. | Not started |

## Human review checkpoints

- After Phase 0: review actual LubeLogger architecture.
- After Phase 1: approve the reconciled system specification and architecture.
- After UI foundation (Phase 2): visually inspect the new design direction. ✅ done — token foundation + shell/nav consolidation user-verified live; remaining Phase 2 polish deferred to per-screen rollout in later phases.
- After Parts + Planned Work (Phases 5–6): verify the core domain model. ✅ done — both phases user-verified live, including Phase 6's drag-and-drop Kanban interactions. **← proceeding to Phase 7**
- After Planned Work → Service Record (Phase 7): end-to-end acceptance test. ✅ done — idempotency and full ImportMode coverage curl-verified, user confirmed no UI regression.
- After Government Data (Phase 8): mocked adapters + Dashboard panel curl-verified. ✅ done, user approved moving to Phase 9.
- After Mileage/Odometer (Phase 9): Source provenance + regression warning curl-verified. ✅ done, user approved moving to Phase 10.
- After Documents (Phase 10): DocumentType categorization curl-verified, including the MVC-save bug fix. ✅ done, user approved moving to Phase 11.
- After Global Search (Phase 11): cross-vehicle search curl-verified, technical decision recorded in ARCHITECTURE.md. ✅ done. **← awaiting go-ahead before Phase 12**
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
