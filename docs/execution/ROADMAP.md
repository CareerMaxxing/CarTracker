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
| 16 | Sidebar App Shell & Dashboard Redesign | Restructure navigation from a top tab-strip + all-vehicles Garage-grid homepage to a persistent left sidebar, matching a mockup's editorial aesthetic (already close in spirit to the existing Zara + Magneto design system). Intentionally reverses UI_TRANSITION.md's explicit "keep Home/Vehicle nav separate" decision, with the user's sign-off. Visual/layout pass only this phase — new widgets use real existing data; Trips/fuel-gauge/month-over-month%/fleet-stats are flagged as candidate future phases, not built now. | 🟡 In progress — Increments 1-3b complete (nav port + vehicle switcher). Increment 4 (auto-redirect landing page) built, then **explicitly reverted per direct user feedback** — Garage grid stays the homepage, with a "Jump to Vehicle" quick-switcher on both sidebars instead. Increment 5 (Dashboard hero rebuild) complete — found and fixed a real gap (photo-less vehicles showed no hero/sold-indicator at all). Increment 6 (Quick Actions tile grid) complete — found and fixed a real bug before shipping: 3 of 4 planned tiles would have silently done nothing on first page load, since their target modals live inside lazily-loaded tab partials never visited yet (PHASE_16.md). Increments 7-11 (Fuel Economy/Total Spent/Planned Maintenance/Recent Activity widgets, promo tile) still open |

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
  data, independently verified by the agent, not just the user's report. Increment 3 (Tailscale)
  complete — https://legion.tail80af14.ts.net/ verified reachable from the phone over mobile data
  with home wifi off, proving genuine tailnet reachability. Increment 4 (auth) explicitly declined by
  the user (device-level protection + Tailscale's own device authorization judged sufficient).
  Increment 5 (PWA install) complete — installed via Chrome's "Install app" flow, Samsung
  battery/VPN settings tuned so the always-running background connection doesn't get silently killed,
  user confirmed the home-screen icon opens the real app full-screen. **Phase 15 complete.**
- Phase 16 (Sidebar App Shell & Dashboard Redesign): user shared a dashboard mockup and asked to plan
  matching its editorial look. Explicitly chose the full sidebar/IA restructure (not just a Dashboard
  reskin) and a "real data only" scope for new widgets (Trips/fuel-gauge/month-over-month%/fleet-stats
  flagged as future-phase candidates, not built now) via AskUserQuestion. Increment 1
  (CurrentVehicleId plumbing) complete, verified via a real curl round-trip, zero visual change yet.
  Increment 2 (shared sidebar shell, Home/Index only) structurally complete - build, tests, and
  curl-inspected markup/CSS all green; caught and fixed a real bug along the way (the old
  overflow-collapse JS would have spun in an infinite 500ms retry loop with no horizontal tab list
  left to measure). One design choice flagged for the user rather than guessed at: active-nav
  indicator is a left border accent bar, not the mockup's dot, reasoned from the app's flat/sharp-
  corner design language. User reviewed live and said "better, carry on" - read as approval, no
  specific changes requested. Increment 3 (Vehicle/Index ported onto the same shell): structurally
  complete, verified against the real vehicle - preserved TabOrder drag-and-drop ordering, VisibleTabs
  per-tab visibility gating, and the live reminder-bell urgency icon, all found by reading the actual
  helper/JS behind them rather than assumed. Split the vehicle-switcher out to its own increment (3b)
  instead of bundling it with the nav port, since Vehicle's nav was already the riskiest single change
  in this phase. User reviewed live and said "bang on, continue" - clear approval. Increment 3b
  (vehicle switcher): reused the existing GetVehicleSelector filtering pattern (built for an unrelated
  "duplicate record to another vehicle" feature) rather than re-deriving vehicle-access rules;
  discovered along the way that this household has two real vehicles, the first genuine multi-vehicle
  test case this phase has exercised. User said "continue" (read as approval). Increment 4 (landing
  page routes to the current vehicle's Dashboard): caught a real design trap before coding it - an
  unconditional redirect would have made the Garage grid permanently unreachable, since every "back to
  Home" link would just bounce forward again. Solved with an explicit showGarage=true opt-out,
  grepping the whole wwwroot tree first to find every existing bare-/Home navigation (login.js,
  vehicle.js's delete flow) and deliberately leaving those two unchanged. Verified against 4 real
  scenarios (default redirect, explicit garage bypass, respects a specific stored preference,
  invalid-id fallback), not just the happy path. User's answer to the real-UX-review ask: **"open on
  the grid, but leave the option to transition to other cars in the corner"** - a deliberate decision,
  not a bug report. Reverted cleanly (Index() back to a bare return View(), returnToGarage() back to
  plain /Home, the showGarage plumbing removed entirely rather than left dormant) and added the
  Increment 3b vehicle switcher to Home/Index's sidebar too (labeled "Jump to Vehicle" there, no
  "current" vehicle context to be "switching" relative to on the Garage page). CurrentVehicleId itself
  and the switcher's set-and-navigate behavior were never part of what got reverted. Increment 5
  (Dashboard hero rebuild): reused an existing design decision already articulated elsewhere in this
  codebase (a CSS comment on the same photo band already reasoned that repeating the vehicle's
  identity in the hero would be "redundant, not confident" since the sidebar already shows it) rather
  than re-deciding whether to add a title. Found a real gap while reading the code to plan the change:
  vehicles without a photo showed no hero band AND no sold indicator at all (the sold band was nested
  inside the photo-only conditional). Fixed with a placeholder reusing .ct-empty-state's established
  muted-icon language, verified against a throwaway test vehicle (not just the two real ones, which
  both happen to have photos), cleaned up afterward. User looked at the live result and asked "where
  are the differences I should be seeing?" - fair question, since Increment 5's fix doesn't apply to
  either real vehicle (both have photos). Answered directly: most of what's visible so far is
  Increments 1-4 (the sidebar); confirmed via AskUserQuestion to continue into the increments that
  actually change the Dashboard's content. Increment 6 (Quick Actions tile grid): found and fixed a
  real bug before it shipped, not after - 3 of the 4 planned tiles (Add Fuel/Service/Planned Work)
  would have silently done nothing on a fresh page load, because their target "add" modals live inside
  each record type's own lazily-loaded tab partial (never visited yet, since Dashboard is the default
  tab), not in Vehicle/Index.cshtml itself - only the Reminder tile's modal happens to live there
  directly. Fixed with an optional callback param on the existing tab-loader functions (fetch the
  target tab's content first, which populates its modal shell, then open the modal) rather than
  duplicating fetch logic or switching the visible tab. Verified against real endpoint responses, not
  just re-read the markup. **Next: Increments 7-11 - Fuel Economy sparkline, Total Spent, Planned
  Maintenance, Recent Activity, and a Magneto-motif promo tile.**

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
