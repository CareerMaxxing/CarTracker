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
| 12 | Local Reliability / Offline Hardening | Reliable startup, DB integrity, attachment integrity, backup, restore, export/import, recovery from interrupted operations. | ✅ Complete — found and fixed two real data-loss/orphan bugs affecting Part/PartPurchase (missing from cleanup + vehicle-deletion cascade), added a broken-attachment-link diagnostic, backup/restore round-trip curl-verified (PHASE_12.md) |
| 13 | AI/OCR | Explicitly deferred. Leave clean extension points only; no feature work. | ✅ Confirmed deferred — no AI/OCR code exists anywhere in the codebase (verified by grep); see PHASE_13.md |
| 14 | V1 Hardening | Unit/integration/UI/migration/duplicate-operation/adapter-failure/backup-restore tests, error handling, accessibility, responsive validation, performance, security review. | 🟡 In progress — Increment 1 (security review): real static-file auth bypass + unrestricted upload fixed. Increment 2 (automated tests): xUnit + WebApplicationFactory project stood up, 10 passing tests. Increment 3 (accessibility): all 41 modals audited, aria-labelledby wired on 39, 2 real duplicate-id bugs found and fixed (PHASE_14.md). Icon buttons/keyboard-nav/labels/mobile/performance still open |
| 15 | Remote Access & Persistent Hosting | Reach the existing app from the user's phone with the same live data as the PC, over a private Tailscale network — no new sync architecture, single server + existing SignalR live updates. Revisits CLAUDE.md's localhost-only/no-cloud-deployment decisions with the human owner's explicit sign-off. | 🟡 In progress — Increment 1 (Windows Service hosting readiness): CWD-relative data-path fix + DataProtection key persistence, build/tests/health-check all green. Increment 2 (publish + carry over real data + register as a Windows Service bound to 127.0.0.1:5299): complete, independently verified RUNNING with real data (PHASE_15.md). Increment 3 (Tailscale on PC + phone, verify remote reachability) needs the user present with their phone — not started. Auth enablement and PWA install verification also open |

## Human review checkpoints

- After Phase 0: review actual LubeLogger architecture.
- After Phase 1: approve the reconciled system specification and architecture.
- After UI foundation (Phase 2): visually inspect the new design direction. ✅ done — token foundation + shell/nav consolidation user-verified live; remaining Phase 2 polish deferred to per-screen rollout in later phases.
- After Parts + Planned Work (Phases 5–6): verify the core domain model. ✅ done — both phases user-verified live, including Phase 6's drag-and-drop Kanban interactions. **← proceeding to Phase 7**
- After Planned Work → Service Record (Phase 7): end-to-end acceptance test. ✅ done — idempotency and full ImportMode coverage curl-verified, user confirmed no UI regression.
- After Government Data (Phase 8): mocked adapters + Dashboard panel curl-verified. ✅ done, user approved moving to Phase 9.
- After Mileage/Odometer (Phase 9): Source provenance + regression warning curl-verified. ✅ done, user approved moving to Phase 10.
- After Documents (Phase 10): DocumentType categorization curl-verified, including the MVC-save bug fix. ✅ done, user approved moving to Phase 11.
- After Global Search (Phase 11): cross-vehicle search curl-verified, technical decision recorded in ARCHITECTURE.md. ✅ done, user approved moving to Phase 12.
- After Local Reliability (Phase 12): two real Part/PartPurchase reliability bugs found and fixed, curl-verified. ✅ done, user approved moving to Phase 13.
- Phase 13 (AI/OCR): confirmed deferred, no implementation, per CLAUDE.md's locked decision. ✅ done.
- Phase 14 (V1 Hardening) Increment 1 (Security Review): user chose this as first priority; found and fixed a real static-file auth bypass + unrestricted upload, both approved by the user and curl-verified. ✅ done.
- Phase 14 Increment 2 (Automated Tests): user chose this next; stood up the xUnit/WebApplicationFactory project deferred since Phase 7, 10 tests passing covering the highest-risk flows identified across prior phases. ✅ done.
- Phase 14 Increment 3 (Accessibility - modals): background code audit found 4 issue categories; user chose the narrowest scope (modal aria-labelledby only). All 41 modals audited, 39 wired correctly, 2 real duplicate-id bugs fixed along the way. ✅ done. **← next: more Phase 14 increments (icon-button labels/keyboard-nav/mobile/performance) or user's next priority**
- Before real DVLA/DVSA adapters: approve credential/API architecture.
- Before V1: final system review.
- Phase 15 (Remote Access & Persistent Hosting): user explicitly authorized revisiting the
  localhost-only/no-cloud-deployment locked decisions in conversation and chose Tailscale over
  LAN-only or public-internet hosting. Increment 1 (Windows Service hosting readiness) complete,
  build/tests/health-check verified. Increment 2 (publish, carry over real data, register as a
  Windows Service bound to 127.0.0.1:5299) complete — service confirmed RUNNING with real vehicle
  data, independently verified by the agent, not just the user's report. **← next: Increment 3 needs
  the user present with their phone to install Tailscale on both devices and verify remote
  reachability.**

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
