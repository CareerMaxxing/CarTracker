# DEFERRED.md

Consolidated tracking of everything intentionally deferred during phase work, per the user's
2026-08-17 decision: these are left as finishing-touches items to revisit at the end (naturally
dovetails with Phase 14 — V1 Hardening) rather than forgotten. Each entry links back to the phase
doc that first identified it, with enough context to pick back up cold.

## UI polish (from Phase 2 — UI Design System)

- **Setup wizard's step nav duplication** — `Home/Setup.cshtml`'s setup-wizard step navigation
  renders as a desktop button row and a separate mobile `<select>` dropdown (6 items each,
  hand-duplicated). Smaller and structurally different from the tab-bar triplication fixed in
  Phase 2 (not driven by `checkNavBarOverflow()`), lower priority. See `PHASE_02.md`.
- **Typography scale** — never touched; no evidence surfaced that the existing
  `html { font-size: 14px / 16px }` responsive base needs to change. Revisit only if a real need
  appears.
- **Forms/tables/dialogs styling** — the design-token foundation (spacing/radius/shadow/motion) was
  landed in Phase 2, but not applied beyond `.card`/`.taskCard`/`.kiosk-card`. Buttons, form
  controls, tables, and modals still use Bootstrap defaults + ad hoc `site.css` rules.
- **`.status-badge` / `.ct-empty-state` primitive adoption** — defined in Phase 2's `UI_SPEC.md`.
  `.ct-empty-state` has since been adopted (Documents tab in Phase 4, Parts tab in Phase 5).
  `.status-badge` is still unused anywhere — candidate uses: reminder urgency, plan priority, future
  MOT status.
- **`prefers-reduced-motion` support** — no CSS animation in the codebase (card hover-scale,
  bell-shake, table-row-shake, mobile-nav slide-in) respects this media query. Explicitly an
  accessibility item, natural fit for Phase 14.

## Dashboard (from Phase 3 — Garage / Dashboard)

- **Richer upcoming-work/reminder summarization** — the Garage card's reminder indicator is
  currently a binary bell icon (has urgent/past-due reminders or doesn't), not a count or "next due"
  summary. The original spec's "Upcoming work" bullet arguably wants more than this. Larger change
  than a single badge (touches `ReminderHelper`'s urgency computation and card layout). See
  `PHASE_03.md`.
- **`checkNavBarOverflow()` candidate bug** (`wwwroot/js/shared.js` ~line 1982) — the function hides
  overflowing primary nav items and reveals their "more"-dropdown twins, but never calls `.show()`
  on the `.nav-item-more` toggle `<li>` itself when overflow occurs (only ever hidden, in the
  no-overflow branch). A speculative fix was written and then reverted since it was never verified
  against a genuine overflow scenario — the reported bug that prompted the investigation turned out
  to be unrelated (default `VisibleTabs` config, not a bug at all). **To fix properly**: enable
  several tabs in Settings → Visible Tabs, narrow the browser until the "•••" menu should appear,
  confirm it's broken, re-apply the one-line fix, verify the dropdown works. See `PHASE_03.md`.

## Parts domain (from Phase 5 — Parts Domain)

- **Consumption/restoration wiring** — `PartPurchase.QuantityRemaining` and `RequisitionHistory`
  exist on the model but nothing decrements/restores them yet. Making Service/Gas/Plan records able
  to consume from a `PartPurchase` (mirroring today's `SupplyUsage`/`SupplyUsageHistory` mechanism
  for `SupplyRecord`) is real additional scope — new UI in those records' modals, new controller
  logic mirroring `RequisitionSupplyRecordsByUsage`/`RestoreSupplyRecordsByUsage` in
  `Controllers/Vehicle/SupplyController.cs` / `Logic/VehicleLogic.cs`. See `PHASE_05.md`.
- **`ImportMode`/`ExtraFields`/Documents-aggregator/CSV-import integration** — Part/PartPurchase
  don't have an `ImportMode` value, so they don't show up in the Phase 4 Documents tab, the
  custom-field admin UI, or CSV import/export, and the Parts tab's list view is deliberately simpler
  than the older tabs (no column customization, bulk actions, tag-filter toolbar) as a result. Adding
  `ImportMode` values is cheap but ripples across several switch statements/aggregators — do it when
  a concrete need shows up (e.g. wanting Parts to appear in Documents).
- **Fitment/engine associations beyond shop-wide** — FR-PART-03's fuller vision (a part explicitly
  associated with specific engines/fitments independent of any one vehicle) isn't modeled; the
  current `VehicleId=0` shop-wide convention (inherited from `SupplyRecord`) is the interim.
- **Dedicated Parts catalog screen** — no way to browse/edit the full Part catalog or see a part's
  price history across vehicles in the UI yet (the API supports both -
  `GET /api/vehicle/parts/all`, `GET /api/parts/purchases?partId=`). Only inline quick-add-during-
  purchase exists. A real catalog browse/edit screen (plus surfacing price history somewhere) is a
  natural next increment whenever the Parts UI gets revisited.

## Explicitly declined, not deferred (do not resurrect without a fresh ask)

- **Regrouping vehicle nav into the six "Vehicle Experience" categories** (Overview/Maintenance/
  History/Parts/Projects/Documents) — the user explicitly chose to keep the current 13+-tab
  structure in Phase 4 rather than restructure navigation. This is a decision, not an oversight.
