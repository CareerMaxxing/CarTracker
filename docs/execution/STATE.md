# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 5 — Parts Domain
Current task:       PHASE-05 (see docs/execution/PHASE_05.md) — Part/PartPurchase, backend+API+UI
Status:             Complete. Backend/API verified via curl, UI verified via curl then confirmed
                     live in the user's browser (full workflow: quick-add a part inline, record a
                     purchase, see it in the list, edit, delete).
Last completed:      Phase 4 finished (browsable Documents tab, see PHASE_04.md).
                     Phase 5: split SupplyRecord's conflated "part + purchase" into two new,
                     additive entities (Part = global catalog, PartPurchase = vehicle-scoped
                     transaction, VehicleId=0 shop-wide) - SupplyRecord untouched, zero regression
                     risk. Increment 1: full LiteDB+Postgres data access + /api/parts/* and
                     /api/vehicle/partpurchases/* endpoints, matching every existing convention.
                     Verified the core design goal via curl: same Part purchased twice at different
                     prices, price-history endpoint returns both correctly. Increment 2: new "Parts"
                     tab (15th tab, always-visible like Documents) with a Part-picker dropdown in the
                     purchase modal and an inline "+" quick-add-part flow so users never have to
                     leave the purchase form to catalog a new part. Caught and fixed a latent bug
                     before shipping: QuantityRemaining was being reset to full Quantity on every
                     edit (harmless today since nothing consumes it yet, but would have silently
                     undone tracked consumption once a future increment wires that up) - fixed by
                     making new-vs-edit handling explicit in the controller.
Next task:           User decision (2026-08-17): all deferred items accumulated so far are
                     intentionally left for a finishing-touches pass at the end (see
                     docs/execution/DEFERRED.md - the consolidated list, replaces tracking individual
                     deferred items per-phase in this file going forward) rather than being
                     addressed now. Proceed to Phase 6 (Planned Engineering Work) next per
                     ROADMAP.md.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow with the user (implement + diff/API-verify, they confirm live)
                        continues to work well for interactive-markup work.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items (UI polish, dashboard richness, Parts domain
                        follow-ups, the shared.js checkNavBarOverflow() candidate bug) - all deferred
                        to a finishing-touches pass, not forgotten.
Open decisions:      None pending - proceeding to Phase 6 per explicit user instruction.
Do not:              Start any phase beyond Phase 6 without the user's go-ahead, per CLAUDE.md's
                     phase-boundary rule. Do not re-litigate or re-surface items already tracked in
                     DEFERRED.md as if they were forgotten - they're intentionally parked. Do not
                     assume SQLite is available anywhere in this codebase. Do not treat "Planned
                     Work -> Service Record" as unbuilt - it exists and needs hardening (Phase 7,
                     itself likely a DEFERRED.md-style finishing touch given how small the actual gap
                     is). Do not assume a fresh
                     vehicle/user has any tabs visible beyond Dashboard - VisibleTabs defaults to
                     [Dashboard] only; if testing anything on a vehicle detail page, check/set
                     VisibleTabs first or use the API directly to avoid re-discovering this "vehicle
                     nav is broken" false alarm. When touching interactive markup, keep using the
                     diff-verify-then-user-confirms workflow. When calling record-add API endpoints
                     for testing, field names/casing are inconsistent across export models (e.g.
                     servicerecords/add wants "odometer" not "mileage"; dates must match the server's
                     locale format, dd/mm/yyyy here, not US-style) - check the relevant *ExportModel
                     class in Models/Shared/ImportModel.cs first rather than guessing. Part is NOT
                     vehicle-scoped (global catalog) but PartPurchase IS (VehicleId, 0=shop-wide) -
                     don't conflate the two when extending this domain further. PartPurchase.
                     QuantityRemaining must be set explicitly by the caller (full Quantity for new,
                     preserved from the existing record for edits) - never let ToPartPurchase() set
                     it, see the comment on that method.
Last validation:     dotnet build (0 errors, 224 warnings - 209 pre-existing + 15 new, same
                     nullable-reference pattern already present in every other Postgres data-access
                     file, not a new category of issue); full Part/PartPurchase CRUD + UI workflow
                     verified via curl (cross-vehicle price history, shop-wide purchases, quick-add-
                     part-then-purchase flow) and then confirmed live in the user's browser; server
                     start + /health verified green throughout; user's real vehicle confirmed
                     untouched — 2026-08-17.
Last commit:         9749bc9 — "Phase 5: Parts UI - new tab, purchase modal, inline part quick-add"
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
