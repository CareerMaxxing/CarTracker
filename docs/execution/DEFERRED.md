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

## Planned Engineering Work (from Phase 6 — Planned Engineering Work)

- **CSV import/export column mapping for `ActualCost`** — the field exists end-to-end
  (model/input/API) and the add/edit modal captures it, but `PlanRecordExportModel`'s CSV sample
  generator and the CSV column mapping in `Controllers/Vehicle/ImportController.cs` weren't
  extended for it. Round-tripping a plan record through CSV export/import currently loses its
  `ActualCost` value. See `PHASE_06.md`.

## Government Data (from Phase 8 — Government Data)

- **MOT-status Garage dashboard badge** — Phase 3's `DashboardMetric` opt-in badge system
  (`ActiveProjects` etc.) is a natural extension point for an MOT-status badge now that Phase 8
  provides `IDVLAAdapter.GetVehicleData(...).MotStatus`, exactly as flagged as blocked-on-Phase-8 in
  `PHASE_03.md`. Not built yet — touches a different, already-shipped feature (`HomeController`/
  `_GarageDisplay.cshtml`/`_VehicleModal.cshtml`), kept as a separate increment rather than
  bundling into Phase 8's own scope. See `PHASE_08.md`.
- **Real DVLA/DVSA adapter swap** — `IDVLAAdapter`/`IDVSAAdapter` were deliberately shaped so a real
  HTTP-backed implementation is a drop-in replacement for `MockDVLAAdapter`/`MockDVSAAdapter` (same
  registration-number-only input, same DTO shapes). Explicitly not started — needs real API
  credentials and is a mandatory stop condition per `CLAUDE.md` ("Use real DVLA/DVSA credentials" /
  "A new external service... is required").

## Mileage / Odometer (from Phase 9 — Mileage / Odometer)

- **Regression flagging on the other auto-insert forms** — `IsSuspiciousMileageRegression` is only
  wired into the dedicated Odometer tab's manual entry form. Adding a mileage value that regresses
  through the Gas/Service/Repair/Upgrade/Inspection/Plan-completion forms doesn't warn today. Real
  additional scope (each of those ~8 controllers/JS files would need its own warning surface, not
  just a shared logic call), deferred rather than bundled into Phase 9. See `PHASE_09.md`.
- **Source CSV column** — `OdometerRecordExportModel.Source` is exposed read-only via the API's
  JSON GET/list endpoints, but not wired into the CSV import/export column mapping in
  `Controllers/Vehicle/ImportController.cs` (same category of gap as Phase 6's `ActualCost` CSV
  column, deferred there for the same reason - narrower than the phase's core acceptance criteria).
- **Source column in the Odometer tab's visible-columns system** — Phase 9 added a small provenance
  icon+tooltip inline in the Notes cell instead of a full sortable/toggleable table column (which
  would need drag-reorder support, `UserColumnPreferences` persistence, and CSV export wiring - real
  additional scope for a single field). See `PHASE_09.md`.
- **MOT-sourced odometer readings** — `OdometerRecordSource.MOT` exists in the enum (per FR-ODO-01's
  named example) but nothing sets it yet; Phase 8's `IDVSAAdapter.GetMotHistory(...)` returns
  `OdometerValue` per MOT test, which could be auto-inserted as odometer history in a future
  increment, mirroring the existing auto-insert pattern.

## Documents (from Phase 10 — Documents)

- **Bulk re-categorization** — no way to select multiple existing attachments and set their
  `DocumentType` in one action; each file's type is set one at a time via the rename dialog. Fine
  for now given most vehicles have a modest number of attachments; worth a bulk action if that stops
  being true.
- **Dedicated document-management screen** — the Documents tab (Phase 4/10) lists and filters
  documents but attachments are still added/removed from within each record's own modal, not from
  the Documents tab itself. A "manage documents" screen with its own upload flow is a bigger UI
  change than this phase's categorization scope.

## Global Search (from Phase 11 — Global Search)

- **Deep-linking to a specific record across a cross-vehicle navigation** — clicking a same-vehicle
  search result opens that record's edit modal directly (via the existing `CheckRecordExist` +
  tab-switch + modal-open flow); clicking a cross-vehicle result only navigates to the right vehicle
  and tab, not the specific record within it. Replicating the auto-open behavior after a full page
  navigation would need query-param-driven modal-opening logic that doesn't exist today - real
  additional scope, not required for the core "find it across vehicles" acceptance criteria. See
  `PHASE_11.md`.
- **Global Part-catalog-only matches** — search covers `PartPurchase` (vehicle-scoped, has a real
  navigation target) but not standalone `Part` catalog entries with no purchase on any vehicle yet
  (no vehicle context to navigate to, and no catalog browse screen exists to land on either - see the
  already-deferred "Dedicated Parts catalog screen" item from Phase 5). Revisit together if that
  screen ever gets built.

## Local Reliability (from Phase 12 — Local Reliability / Offline Hardening)

- **Broader DB-corruption detection/repair** — the health check remains connectivity-only
  (NFR-REL-02 explicitly says no change required); no deeper data-consistency scan (e.g. orphaned
  foreign-key-style references beyond PartPurchase, which was fixed) was attempted. Revisit only if
  a real corruption incident happens.
- **Postgres backend backup coverage** — `MakeBackup`/`RestoreBackup` are file-based and were never
  wired up for the Postgres backend (pre-existing, not introduced by Car Tracker). Out of this
  phase's scope (NFR-REL-01 named Part/Purchase/Government-adapter data specifically), but worth
  flagging for whoever eventually hardens the Postgres path.

## V1 Hardening (from Phase 14 — Security Review increment)

- **CSRF token infrastructure** — no anti-forgery tokens found on MVC cookie-session endpoints.
  Plausible-by-design (this app is built API-first with Basic-Auth/API-key support on nearly every
  write action - a different threat model than a typical browser-only app) but not deeply verified
  either way. Worth a dedicated look if this app is ever exposed beyond a trusted local network.
- **Content-Security-Policy header** — none set anywhere. Would be useful defense-in-depth (the
  stored-XSS angle this phase closed via upload-extension blocking wouldn't have needed the fix to
  begin with, and any future similar gap would be mitigated). Not implemented since it requires
  auditing every inline script/style the app already uses to avoid breaking the UI - real additional
  scope, not a quick add.
- **Remaining Phase 14 areas** — automated tests, accessibility, mobile/responsive validation, and
  performance review are all still open (see `PHASE_14.md`'s framing note - Phase 14 takes
  increments rather than one pass). Not deferred/declined, just not yet started.

## Test infrastructure (from Phase 7 — Planned Work → Service Record)

- **Automated test project** — ✅ built in Phase 14 Increment 2 (`Tests/CarCareTracker.Tests.csproj`,
  xUnit + `WebApplicationFactory`), no longer deferred. 10 tests passing, covering Phase 7's
  idempotency fix, both Phase 12 Part/PartPurchase reliability bugs, Phase 14's upload-extension
  blocklist, and Phase 9's odometer regression warning. See `PHASE_14.md` Increment 2 for what it
  covers, how isolation from the real `data/` directory works, and two content-root-resolution
  quirks (`WebApplicationFactory`'s layout assumption, `dotnet test`'s VSTest CWD behavior) that
  weren't anticipated in the original investigation below but were resolved.
  - **Not yet covered**: everything else. This is a starting point, not exhaustive coverage - add
    tests incrementally as future work touches other areas, rather than a single dedicated pass.
  - Original Phase 7 investigation findings (superseded by the above, kept for history): `DbName`'s
    CWD-relative resolution, the need for `public partial class Program`, and the parallel-test-safety
    hazard (xUnit parallelizes test classes by default) were all confirmed accurate and used as-is;
    the parallel-safety strategy chosen was full `[Collection]`-based serialization via one shared
    `ICollectionFixture`, not per-test temp directories.
- **Gas/Tax CSV import/export awareness for planned work** — `Controllers/Vehicle/ImportController.cs`'s
  CSV column mapping for `PlanRecord` wasn't checked/extended for the two new target types added in
  Phase 7. Narrower gap than the ActualCost one already listed above; same general category.

## Explicitly declined, not deferred (do not resurrect without a fresh ask)

- **Regrouping vehicle nav into the six "Vehicle Experience" categories** (Overview/Maintenance/
  History/Parts/Projects/Documents) — the user explicitly chose to keep the current 13+-tab
  structure in Phase 4 rather than restructure navigation. This is a decision, not an oversight.
