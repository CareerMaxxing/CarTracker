# PHASE_11 — Global Search

## Scope

Per `REQUIREMENTS.md` FR-SEARCH-01: a global, cross-vehicle, cross-entity search covering parts,
service records, planned work, documents, and vehicles - extending the existing single-vehicle
`SearchRecords`/`SearchRecordsByTags` (`VehicleController`), which already do text/tag search across
all record types but scoped to one vehicle at a time.

## Technical decision (required before implementing, per REQUIREMENTS.md)

`REQUIREMENTS.md` flagged three candidate approaches and required recording the choice here:

- **(a) Extend the existing in-application filtering to run across all visible vehicles** — no new
  infrastructure, works identically on both backends.
- (b) LiteDB's built-in field indexing — no FTS-equivalent, so no real benefit over (a) for
  substring search, and doesn't help on the Postgres backend at all.
- (c) A Postgres `tsvector`/GIN-index approach — only works when running on Postgres, which would
  violate the codebase's dual-backend requirement (every feature must work identically on LiteDB and
  Postgres).

**Chosen: (a).** This is a personal, local-first, single-user app with a handful of vehicles and at
most a few hundred records total (see `CLAUDE.md`'s locked scope) — not an enterprise search
problem. In-memory substring filtering across all of a user's visible vehicles is fast enough at
this scale and needs zero new infrastructure or backend-specific code. Recorded in `ARCHITECTURE.md`
per `REQUIREMENTS.md`'s instruction; revisit only if real usage ever shows this doesn't scale.

## Task packet

```
TASK ID: PHASE-11-01
TITLE: Cross-vehicle global search
OBJECTIVE: Let a user search by keyword across every vehicle they can see, not just the one whose
  page they're currently on, covering the same record types the existing per-vehicle search covers
  plus Parts/PartPurchases (not yet included - no ImportMode value) and the vehicles themselves.
INPUTS: Controllers/VehicleController.cs (SearchRecords, SearchRecordsByTags - the ~175-line
  per-vehicle-type switch statement), Models/Shared/SearchResult.cs, Views/Vehicle/
  _GlobalSearchResult.cshtml, Views/Vehicle/Report/_MapSearchResult.cshtml (a second, unrelated
  consumer of SearchResult - tag-based search for the vehicle image-map feature), wwwroot/js/
  vehicle.js (performGlobalSearch, loadGlobalSearchResult), Views/Vehicle/Index.cshtml
  (globalSearchModal markup).
ALLOWED SCOPE: A new cross-vehicle search endpoint reusing the existing per-vehicle-type filtering
  logic (refactored into a shared private helper, not duplicated); Parts/PartPurchase and
  vehicle-level (Make/Model/LicensePlate/Tags) matching, both previously absent from search entirely;
  a "Search All Vehicles" toggle on the existing search modal; cross-vehicle result navigation.
NON-SCOPE: Changing SearchRecordsByTags' behavior (a separate, narrower feature feeding the vehicle
  image-map search, left exactly as-is beyond the type-signature fix forced by the SearchResult
  model change); a dedicated Parts catalog browse screen (still deferred, unrelated to search); deep
  linking to a specific record's edit modal across a full page navigation for a different vehicle
  (only same-vehicle results get that; cross-vehicle results land on the right tab, not the specific
  record - a smaller, well-scoped MVP, not full parity with same-page result clicking).
IMPLEMENTATION REQUIREMENTS:
  - SearchResult.RecordType changes from ImportMode to string (the view/JS already treated it as a
    string via ToString() either way - not a behavioral change) so non-ImportMode result types
    ("Part", "PartPurchase", "Vehicle") fit without adding fake values to the ImportMode enum (which
    drives CSV import/tabs/VisibleTabs and shouldn't gain values with no real record type).
  - SearchResult gains VehicleId/VehicleName so cross-vehicle results carry which vehicle they belong
    to, for both display and click-navigation.
  - GetSearchResultsForVehicle(vehicleId, vehicleName, query, caseSensitive): the exact same
    per-ImportMode-type switch statement that existed before, extracted so both the existing
    single-vehicle endpoint and the new cross-vehicle one share it rather than duplicating ~175
    lines of near-identical code twice.
  - New PartPurchase branch inside that shared helper (not gated by VisibleTabs, matching how the
    Parts tab itself is always visible) - joins each purchase to its Part for a readable description.
  - New SearchRecordsAcrossVehicles endpoint: iterates every vehicle visible to the caller (root sees
    all, others filtered via the existing FilterUserVehicles pattern), adds a vehicle-level match
    (whole-object serialize-and-contains, same technique every other branch already uses) plus calls
    the shared per-vehicle helper for each one.
  - GetSearchResultIcon(string): falls back to the existing GetImportModeIcon for real ImportMode
    values, adds icons for "Vehicle"/"PartPurchase".
  - loadSearchResult(vehicleId, recordId, recordType) (vehicle.js): same-vehicle results behave
    exactly as before (delegates to the existing loadGlobalSearchResult); cross-vehicle results
    navigate to that vehicle's page on the correct tab; Vehicle/PartPurchase results (no per-record
    edit-modal deep link) just switch/navigate to the relevant tab either way.
DELIVERABLES: Cross-vehicle search endpoint, Parts/vehicle coverage, UI toggle, working navigation.
ACCEPTANCE CRITERIA:
  - A keyword present only on vehicle A's service record is found when searching from vehicle B's
    page with "Search All Vehicles" checked, tagged with vehicle A's name.
  - A keyword matching a vehicle's Make/Model/LicensePlate returns a "Vehicle" result even with no
    matching records.
  - A keyword matching a Part's number/description (via a PartPurchase) returns a result.
  - The existing single-vehicle search is unchanged in behavior and route.
  - The real vehicle and its data are unaffected by any of this (read/aggregate-only feature).
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against two throwaway vehicles (created and deleted after, real vehicle
  confirmed untouched): cross-vehicle keyword match across two different record types on two
  different vehicles; vehicle-level match; PartPurchase match; confirmed the existing single-vehicle
  endpoint's behavior and response shape are unchanged.
STOP CONDITION: Acceptance criteria met, verified via curl, changes committed.
```

## What was done

1. Read `REQUIREMENTS.md` FR-SEARCH-01's three candidate technical approaches and picked (a) -
   extending the existing in-app filtering across all visible vehicles - for the reasons above.
   Recorded the decision in `ARCHITECTURE.md`.
2. Read the existing `SearchRecords`/`SearchRecordsByTags` in full: both are single-vehicle-scoped,
   iterate the user's `VisibleTabs` config, and for each visible `ImportMode` fetch that vehicle's
   records and substring-match against each record's full serialized JSON (a simple, working, if
   unindexed, approach already proven in this codebase).
3. Changed `SearchResult.RecordType` from `ImportMode` to `string` and added `VehicleId`/
   `VehicleName`. Confirmed this doesn't change any rendered output (the views already rendered it
   via Razor's implicit `ToString()`), but does require fixing every `RecordType = ImportMode.X`
   assignment across the codebase to add `.ToString()` - caught by the compiler in two places:
   `SearchRecords`'s own switch (expected, being actively refactored) and `SearchRecordsByTags`, a
   separate, unrelated method that also builds `SearchResult` objects for a different feature (the
   vehicle image-map's tag search) - fixed its type-signature-only compile break without touching its
   behavior, and fixed the one other `GetImportModeIcon(result.RecordType)` call site
   (`Report/_MapSearchResult.cshtml`, that feature's own result view) to use the new
   `GetSearchResultIcon` instead (a strict superset of the old icon lookup for real ImportMode
   values, so behavior-preserving there too).
4. Extracted the ~175-line per-ImportMode-type switch into `GetSearchResultsForVehicle`, tagging
   every result with `VehicleId`/`VehicleName`, and added a new PartPurchase branch inside it
   (joining each purchase to its Part catalog entry for a readable description) - Parts weren't
   searchable at all before this, since they have no `ImportMode` value and so never participated in
   the `VisibleTabs`-driven switch (a known, previously-documented gap from Phase 5).
5. Rewrote `SearchRecords` as a thin wrapper around the shared helper (identical behavior/route/
   response shape to before) and added `SearchRecordsAcrossVehicles`: resolves the caller's visible
   vehicles (root sees all; others via the existing `FilterUserVehicles` pattern already used
   elsewhere, e.g. Phase 5's `GetPartPurchasesByPartId`), adds a vehicle-level match per vehicle
   (Make/Model/Year/LicensePlate/Tags, via the same whole-object-serialize-and-contains technique
   every other branch already uses - no new matching logic invented), then calls the shared helper
   per vehicle.
6. Extended the existing `globalSearchModal` (opened via the "Search" button already present on every
   vehicle's Dashboard tab) with a "Search All Vehicles" toggle, persisted in `localStorage`
   alongside the two existing toggles, rather than building a new search UI/entry point from scratch.
7. Added `loadSearchResult(vehicleId, recordId, recordType)` in `vehicle.js`: same-vehicle results
   delegate to the existing `loadGlobalSearchResult` unchanged; cross-vehicle results navigate via
   `/Vehicle/Index?vehicleId=X&tab=Y` using a small tab-name lookup table (matching the actual tab-id
   prefixes defined in `Index.cshtml`'s `vehicleNavTabs` list); "Vehicle"/"PartPurchase" results (no
   per-record edit-modal equivalent to deep-link into) just switch/navigate to the relevant tab in
   both cases, never attempting the `CheckRecordExist`-based modal-open flow that only applies to the
   13 real record types.
8. Verified via curl against two throwaway vehicles (created and deleted after, real vehicle
   confirmed untouched):
   - A distinctive keyword on vehicle A's service record and vehicle B's note record both surfaced
     via `SearchRecordsAcrossVehicles`, each correctly tagged with its own vehicle's name and
     carrying the right `vehicleId`/`recordId`/`recordType` for the `onclick` handler.
   - A keyword matching only a vehicle's Make/Model returned a "Vehicle"-typed result with no
     matching records.
   - A Part's number/description, reachable only via its `PartPurchase` on one vehicle, returned a
     correctly-labelled "PartPurchase" result.
   - The existing single-vehicle `SearchRecords` endpoint's response was unchanged for the same
     keyword/vehicle.
   - Rendered the vehicle page HTML and confirmed the new "Search All Vehicles" toggle is present.
   - `dotnet build`: 0 errors throughout.

## Result

Complete. `REQUIREMENTS.md` FR-SEARCH-01 satisfied, including two previously-uncovered categories
(Parts and vehicles themselves) that weren't part of the original single-vehicle search at all.
