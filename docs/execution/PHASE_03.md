# PHASE_03 — Garage / Dashboard

## Baseline: what already exists (checked before planning any work)

`HomeController.Garage()` + `Views/Home/_GarageDisplay.cshtml` already implement most of this
phase's target ("what's happening with my cars?"):

- **Vehicle overview cards**: EXISTING — image, year/make/model, identifier, tags, sold banner.
- **Current mileage**: EXISTING — `LastReportedMileage`, shown as a badge.
- **Reminders**: EXISTING — a bell-icon badge (`HasReminders`) when a vehicle has urgent/past-due
  reminders.
- **Cost summaries**: EXISTING — `CostPerMile` and `TotalCost` badges.
- All four of the above are gated by a per-vehicle opt-in list, `Vehicle.DashboardMetrics: List<
  DashboardMetric>` (enum: `Default` covers mileage+reminders, `CostPerMile`, `TotalCost`), edited
  via checkboxes in `Views/Vehicle/_VehicleModal.cshtml` under `#collapseMetricInfo`. The checkbox
  values are collected generically by `shared.js:108` (`$("#collapseMetricInfo :checked")`, mapped
  by `.value`) — adding a new metric only requires a new checkbox with the right `value`, no JS
  changes.
- **MOT status**: NOT PRESENT — correctly so, since it depends on the DVLA/DVSA adapters that don't
  exist yet (Phase 8). Not implemented here; starting Phase 8 early to unblock this would violate
  `CLAUDE.md`'s phase-boundary rule. Left as a documented gap for Phase 8 to close later.
- **Active projects**: NOT PRESENT on the Garage cards at all, despite `PlanRecord` (Planner)
  already existing as a full domain entity (`docs/DATA_MODEL.md`). This is the one genuine, buildable
  gap identified for this phase.

## Task packet

```
TASK ID: PHASE-03-01
TITLE: Active-projects dashboard metric
OBJECTIVE: Add an opt-in "active projects" indicator to Garage vehicle cards, following the exact
  existing DashboardMetric pattern (Default/CostPerMile/TotalCost) rather than inventing a new one.
INPUTS: Enum/DashboardMetric.cs, Controllers/HomeController.cs (Garage action),
  Models/Vehicle/VehicleViewModel.cs, Views/Home/_GarageDisplay.cshtml,
  Views/Vehicle/_VehicleModal.cshtml, External/Interfaces/IPlanRecordDataAccess.cs.
ALLOWED SCOPE: The six files above. No changes to PlanRecord itself, no changes to the Planner tab,
  no new data-access methods (GetPlanRecordsByVehicleId already exists and is sufficient).
NON-SCOPE: MOT status (blocked on Phase 8), a dedicated "active projects" list/modal beyond the
  count badge, any change to what counts as "active" for planning purposes beyond
  Progress != Done.
IMPLEMENTATION REQUIREMENTS:
  - New enum value DashboardMetric.ActiveProjects.
  - HomeController.Garage(): count non-Done PlanRecords per vehicle only when the vehicle opted in,
    matching the existing conditional-computation pattern for the other three metrics.
  - VehicleViewModel.ActiveProjectCount field.
  - New badge in _GarageDisplay.cshtml, same visual treatment as the existing metric badges.
  - New checkbox in _VehicleModal.cshtml's #collapseMetricInfo, matching the existing three.
DELIVERABLES: Working, opt-in active-projects badge on Garage cards.
ACCEPTANCE CRITERIA:
  - A vehicle with the new metric enabled and at least one non-Done plan record shows a count
    badge on its Garage card.
  - A vehicle without the metric enabled shows no such badge (opt-in preserved).
  - A vehicle with the metric enabled but zero active plans shows no badge (don't show a "0").
  - Existing three metrics (mileage/reminders, cost-per-mile, total-cost) unaffected.
  - Enabling the new checkbox in the vehicle edit modal and saving persists it (round-trips through
    existing generic checkbox-collection JS, no JS changes needed).
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via API: create a vehicle, enable ActiveProjects metric, add a PlanRecord with
  Progress != Done, GET /Home/Garage, confirm the badge count in the returned HTML; add a second
  PlanRecord with Progress == Done, confirm the count doesn't include it.
STOP CONDITION: Acceptance criteria met, verified against a running instance, user has reviewed
  live in browser, changes committed.
```

## Investigation detour: "broken" vehicle nav turned out to be default config

While the user was testing this feature, they hit what looked like a serious bug: the vehicle
detail page only showed "Dashboard" and "Search" in its tab bar, no Planner/Odometer/Service
Records/etc., and no "•••" overflow menu either. Investigated thoroughly (confirmed it reproduced
identically on the pre-Phase-2 original nav markup, ruling out the Phase 2 nav consolidation;
confirmed server-rendered HTML was complete and correct; had the user inspect the live DOM via
browser DevTools) before finding the real cause: `StaticHelper.DefaultActiveTab()`
(`Helper/StaticHelper.cs:143-151`) applies `d-none` to any tab button not in
`UserConfig.VisibleTabs`, and a fresh install's `VisibleTabs` defaults to `[Dashboard]` only (from
`appsettings.json`). **This is intended behavior, not a bug** — the user just hadn't yet enabled
more tabs via Garage → Settings → "Visible Tabs" (`Views/Home/_Settings.cshtml:154-223`).

**One real, independent, unverified finding along the way**: `checkNavBarOverflow()`'s
`removeNavbarItems()` in `wwwroot/js/shared.js` (~line 1982) hides overflowing primary nav items
and reveals their "more"-dropdown twins, but never calls `.show()` on the `.nav-item-more` toggle
`<li>` itself (it's only ever `.hide()`-den, in the no-overflow branch) — meaning if genuine
overflow occurs (many `VisibleTabs` enabled + a narrow viewport), users may lose access to the
hidden tabs entirely with no way to open the dropdown that contains them. A speculative fix was
written and then **reverted** since it was never actually verified against a real overflow
scenario (this session's investigation turned out not to need it). **Candidate bug for a future
task**, not fixed here — reproduce with several `VisibleTabs` enabled and a narrowed browser
window before attempting a fix.

## Deferred (documented, not forgotten)

- **MOT status** — Phase 8 dependency, see above.
- **Upcoming-work detail** — the reminders badge is currently a binary warning icon, not a count or
  "next due" summary. Original spec's "Upcoming work" bullet arguably wants more than this, but
  richer reminder summarization on the dashboard is a larger, separate change (touches
  `ReminderHelper`'s urgency computation and the card layout more significantly) — noted as a
  candidate for a follow-up increment rather than bundled into this one, per "smallest complete
  solution."
