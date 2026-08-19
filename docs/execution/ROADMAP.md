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
| 15 | Remote Access & Persistent Hosting | Reach the existing app from the user's phone with the same live data as the PC, over a private Tailscale network — no new sync architecture, single server + existing SignalR live updates. Revisits CLAUDE.md's localhost-only/no-cloud-deployment decisions with the human owner's explicit sign-off. | ✅ Complete — Windows Service hosting readiness, publish+register bound to 127.0.0.1:5299 with real data carried over, Tailscale reachability (`https://legion.tail80af14.ts.net/`) verified from the phone over mobile data with wifi off, and a proper PWA install on the phone (Chrome "Install app", Samsung battery/VPN settings tuned for reliability) (PHASE_15.md). Auth enablement explicitly declined by the user — device-level protection + Tailscale's own device authorization judged sufficient; not re-raised unless the user brings it up again |
| 16 | Sidebar App Shell & Dashboard Redesign | Restructure navigation from a top tab-strip + all-vehicles Garage-grid homepage to a persistent left sidebar, matching a mockup's editorial aesthetic (already close in spirit to the existing Zara + Magneto design system). Intentionally reverses UI_TRANSITION.md's explicit "keep Home/Vehicle nav separate" decision, with the user's sign-off. Visual/layout pass only this phase — new widgets use real existing data; Trips/fuel-gauge/month-over-month%/fleet-stats are flagged as candidate future phases, not built now. | ✅ Complete — all 11 increments shipped: sidebar shell + vehicle switcher (1-3b), landing-page redirect built then explicitly reverted per direct user feedback (4), Dashboard hero rebuild fixing a real photo-less-vehicle gap (5), Quick Actions tiles fixing a real "3 of 4 would silently do nothing" bug (6), and the four widget-row cards — Fuel Economy sparkline, Total Spent, Planned Maintenance, Recent Activity — plus a promo tile (7-11), each verified against real vehicle data. **Found a real deployment gap after "completion": all 11 increments were only ever verified against the dev instance, never actually deployed to the production service the user's phone reaches — fixed via the same elevated stop/publish/start sequence as Phase 15, independently verified via the real Tailscale URL. User confirmed live on their phone: "spot on."** (PHASE_16.md) |
| 17 | Real MOT History & Advisory Tracking | Pull the vehicle's full MOT history (not just the latest test), extract every advisory/failure across all past tests into tracked Planner items with recurring-advisory detection and a lighter "mark resolved" status. Revisits CLAUDE.md's "mocked DVLA/DVSA adapters only" locked decision with the human owner's explicit sign-off — switches to real DVSA MOT History API data. | ✅ Complete, deployed, and **running on genuinely live DVSA data** — credential plumbing (1), real DVSA adapter proven end-to-end against Microsoft's OAuth endpoint (2), full MOT history UI (3), advisory text normalization (4), Add-to-Planner with recurring advisories grouped (5), orthogonal "mark resolved" status (6), curated-synonym grouping + resolved-status visibility from direct user feedback (8), real-credential activation with a real defects-field-name bug found and fixed by inspecting a raw API response (9), and an orthogonal "Ignored" section in the Planner for insignificant items, kept out of the hardcoded 6-swimlane machinery the same way Resolved was (10). All 56 real defects across the BMW Z4's 22 real MOT tests confirmed live via both localhost and the real Tailscale URL (PHASE_17.md) |

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
- Before real DVLA/DVSA adapters: approve credential/API architecture. ✅ done — user chose real DVSA
  data now (Phase 17); architecture (live-config-read adapter, OAuth2 client-credentials + X-API-Key,
  ServerConfig-pattern credential storage) reviewed by a Plan agent and the agent directly before
  implementation started.
- Before V1: final system review.
- Phase 15 (Remote Access & Persistent Hosting): user explicitly authorized revisiting the
  localhost-only/no-cloud-deployment locked decisions in conversation and chose Tailscale over
  LAN-only or public-internet hosting. Increment 1 (Windows Service hosting readiness) complete,
  build/tests/health-check verified. Increment 2 (publish, carry over real data, register as a
  Windows Service bound to 127.0.0.1:5299) complete — service confirmed RUNNING with real vehicle
  data, independently verified by the agent, not just the user's report. Increment 3 (Tailscale)
  complete — https://legion.tail80af14.ts.net/ verified reachable from the phone over mobile data
  with home wifi off, proving genuine tailnet reachability. Increment 4 (auth) explicitly declined by
  the user (device-level protection + Tailscale's own device authorization judged sufficient).
  Increment 5 (PWA install) complete — installed via Chrome's "Install app" flow, Samsung
  battery/VPN settings tuned so the always-running background connection doesn't get silently killed,
  user confirmed the home-screen icon opens the real app full-screen. **Phase 15 complete.**
- Phase 16 (Sidebar App Shell & Dashboard Redesign): user shared a dashboard mockup and asked to plan
  matching its editorial look, same EnterPlanMode/Explore-agent/AskUserQuestion process as Phase 15.
  Two real design decisions made by the user along the way, not assumed: chose the full sidebar/IA
  restructure over a smaller reskin; then, after Increment 4 built an auto-redirect to the current
  vehicle's Dashboard, explicitly reverted it ("open on the grid, but leave the option to transition to
  other cars in the corner") in favor of a "Jump to Vehicle" quick-switcher instead. All 11 increments
  shipped: sidebar shell + vehicle switcher (1-3b), the redirect-then-revert (4), Dashboard hero rebuild
  (5, fixed a real photo-less-vehicle gap), Quick Actions tiles (6, fixed a real "3 of 4 buttons would
  silently do nothing" bug), and the widget row - Fuel Economy sparkline, Total Spent, Planned
  Maintenance, Recent Activity, promo tile (7-11) - each verified against real vehicle data, including
  an independent cross-widget consistency check (Increment 10's activity costs summing to exactly the
  same £260.00 Increment 8 showed). Full increment-by-increment detail in `PHASE_16.md`. After
  "completion," found a real deployment gap: every increment had only been verified against the dev
  instance, never actually deployed to the production service the user's phone reaches - fixed via the
  same elevated stop/publish/start sequence as Phase 15, independently verified via the real Tailscale
  URL. **User confirmed live on their phone: "spot on." Phase 16 complete.**
- Phase 17 (Real MOT History & Advisory Tracking): user asked to expand MOT tracking (full history,
  Planner items per advisory, recurring-advisory detection, crossed-off resolution). Same EnterPlanMode/
  Explore-agent/AskUserQuestion/Plan-agent-review process as Phase 15/16. Three real decisions made by
  the user, not assumed: (1) switch to real DVSA MOT History API data now rather than staying on the
  mocked adapter - the locked CLAUDE.md decision this phase revisits; (2) MOT-linked Planner items
  resolve via a new lighter "mark resolved" status, not the existing Done→auto-ServiceRecord pipeline;
  (3) advisories already resolved before the feature existed (the user's tyres) get a one-time manual
  cleanup pass, not automated fuzzy-matching. A Plan-agent design review (before implementation started)
  found `PlanProgress` is load-bearing in the Kanban board's six hardcoded swimlanes and the API's
  validation - confirmed the "resolved" concept must be an orthogonal field, not a 7th enum value - and
  confirmed Postgres storage (`PlanRecord` as a single jsonb blob) makes new POCO fields free on both
  backends. **All 6 increments complete**: credential plumbing (1); the real DVSA adapter, live-config-
  selected with a genuine end-to-end failure-path proof against Microsoft's real OAuth endpoint (2);
  full MOT history UI with colour-coded advisories and split Mock badges (3); advisory text
  normalization, built before the linkage actions that depend on it per the Plan-agent review (4);
  Add-to-Planner (single + bulk), deduped and visually grouped by recurring issue - verified live
  against the real BMW Z4 vehicle, whose two recurring advisories correctly collapsed into single rows
  exactly as the user's original tyres example asked for (5); and the orthogonal "mark resolved"
  status, which caught and fixed a real null-safety bug (`GetPlanRecordById` returning `null` for a
  missing id) along the way, logged for the two untouched pre-existing call sites in `DEFERRED.md`
  (6). Full loop verified end-to-end against real dev data, then **deployed to production 2026-08-19**
  - the elevated stop/publish/start sequence exposed a real, pre-existing, Phase-17-unrelated issue:
  production's Kestrel port binding (127.0.0.1:5299, set up in Phase 15) had been silently wiped from
  `serverConfig.json` at some earlier point (most likely an earlier `/setup` save without the HTTPS
  page populated - logged as a candidate hardening item in `DEFERRED.md`), so the service came back up
  on the wrong default port until this was found and fixed. Independently verified via curl against
  both `127.0.0.1:5299` and the real `https://legion.tail80af14.ts.net` Tailscale URL: new code live,
  both real vehicles' data intact. **Phase 17 complete.** Full increment-by-increment detail in
  `PHASE_17.md`.

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
