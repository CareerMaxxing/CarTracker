# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 5 — Parts Domain
Current task:       PHASE-05-01 (see docs/execution/PHASE_05.md) — Part/PartPurchase backend+API
Status:             Backend and API complete, verified end-to-end via curl. UI is a deliberately
                     separate follow-up increment (user's explicit scoping choice), not yet built,
                     so not yet reviewed live.
Last completed:      Phase 4 finished (browsable Documents tab, see PHASE_04.md).
                     Phase 5 increment 1: confirmed the Part/PartPurchase design with the user before
                     writing code (Part = global catalog entity, not vehicle-scoped; PartPurchase =
                     vehicle-scoped transaction entity referencing a Part, VehicleId=0 for shop-wide,
                     mirrors SupplyRecord's existing convention). Built both as new, additive entities
                     - SupplyRecord completely untouched, zero regression risk. Full LiteDB+Postgres
                     data access, /api/parts/* and /api/vehicle/partpurchases/* endpoints, matching
                     every existing convention (CollaboratorFilter, APIKeyFilter, OperationResponse,
                     WebHookPayload.Generic events). Verified via curl: same Part purchased twice at
                     different prices/vehicles, price-history endpoint correctly returns both -
                     validates the core "price belongs to purchase, not part" design goal.
Next task:           Open decision: build the Parts UI (new Vehicle tab/screen) as the natural next
                     increment, or something else - ask the user. Also still open from before:
                     shared.js checkNavBarOverflow() candidate bug (Phase 3, unverified), richer
                     reminder summarization (Phase 3, deferred). Do not start a new phase (Phase 6+)
                     without the user's go-ahead - Phase 5's UI increment is still outstanding first.
Known blockers:      1. No browser/screenshot tool in this environment - the "review locally as I
                        go" workflow with the user (implement + diff/API-verify, they confirm live)
                        is working well and should continue for any further interactive-markup work.
                        Note: Phase 5's backend-only work has NOT been through this loop yet since
                        there's no UI - only curl-verified so far.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet (candidate: Phase 7 idempotency fix).
                     4. Candidate bug in shared.js checkNavBarOverflow() (see PHASE_03.md) - found,
                        not fixed, not verified. Not blocking anything currently since it only
                        manifests under genuine tab overflow, which requires non-default VisibleTabs.
Open decisions:      Build Phase 5's UI next, or something else - pending user input.
Do not:              Start Phase 6 (or any further phase) without the user's go-ahead, per
                     CLAUDE.md's phase-boundary rule - and note Phase 5 itself isn't fully done yet
                     (UI outstanding). Do not assume SQLite is available anywhere in this codebase.
                     Do not treat "Planned Work -> Service Record" as unbuilt - it exists and needs
                     hardening (Phase 7). Do not assume a fresh vehicle/user has any tabs visible
                     beyond Dashboard - VisibleTabs defaults to [Dashboard] only; if testing anything
                     on a vehicle detail page, check/set VisibleTabs first or use the API directly to
                     avoid re-discovering this "vehicle nav is broken" false alarm. When touching
                     interactive markup, keep using the diff-verify-then-user-confirms workflow. When
                     calling record-add API endpoints for testing, field names/casing are inconsistent
                     across export models (e.g. servicerecords/add wants "odometer" not "mileage";
                     dates must match the server's locale format, dd/mm/yyyy here, not US-style) -
                     check the relevant *ExportModel class in Models/Shared/ImportModel.cs first
                     rather than guessing. Part is NOT vehicle-scoped (global catalog) but
                     PartPurchase IS (VehicleId, 0=shop-wide) - don't conflate the two when extending
                     this domain further.
Last validation:     dotnet build (0 errors, 224 warnings - 209 pre-existing + 15 new, same
                     nullable-reference pattern already present in every other Postgres data-access
                     file, not a new category of issue); full Part/PartPurchase CRUD lifecycle
                     verified via curl including cross-vehicle price history and shop-wide purchases;
                     server start + /health verified green throughout; user's real vehicle confirmed
                     untouched — 2026-08-17.
Last commit:         (pending — Phase 5 commit, created immediately after this file)
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
