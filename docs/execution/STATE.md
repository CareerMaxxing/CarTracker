# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 11 — Global Search
Current task:       PHASE-11-01 (see docs/execution/PHASE_11.md) — cross-vehicle search
Status:             Complete. Curl-verified end-to-end on two throwaway vehicles (deleted after):
                     cross-vehicle keyword match across two record types on two vehicles, a
                     vehicle-level (Make/Model) match, a PartPurchase match, and confirmed the
                     existing single-vehicle search endpoint's behavior/route/response are unchanged.
                     Real vehicle (id 1) confirmed untouched. Not yet shown to the user live in
                     browser - offering that before Phase 12.
Last completed:      Phase 10 finished (document type categorization, see PHASE_10.md), user
                     confirmed and approved moving to Phase 11. Phase 11: resolved REQUIREMENTS.md
                     FR-SEARCH-01's open technical decision (extend existing in-app filtering across
                     all visible vehicles - recorded in ARCHITECTURE.md), extracted the existing
                     ~175-line per-vehicle-type search switch into a shared
                     GetSearchResultsForVehicle helper (used by both the unchanged single-vehicle
                     endpoint and the new SearchRecordsAcrossVehicles one), added Parts/PartPurchase
                     and vehicle-level matching (neither searchable before at all), changed
                     SearchResult.RecordType from ImportMode to string so new non-record-type result
                     kinds ("Part"/"PartPurchase"/"Vehicle") don't need fake ImportMode values, and
                     extended the existing search modal with a "Search All Vehicles" toggle rather
                     than building new search UI. The RecordType type change forced a type-signature
                     fix (not a behavior change) in one unrelated pre-existing feature
                     (SearchRecordsByTags / Report/_MapSearchResult.cshtml, the vehicle image-map's
                     own tag search) - fixed and left otherwise untouched.
Next task:           Show the user the "Search All Vehicles" toggle and a live cross-vehicle search,
                     confirm it looks right, then ask before starting Phase 12 (Local Reliability /
                     Offline Hardening) or anything else.
Known blockers:      1. No browser/screenshot tool in this environment - Phase 11's changes are
                        curl-verifiable for correctness but the search modal's toggle and live
                        cross-vehicle result navigation are genuinely worth a look.
                     2. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     3. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. Phase 11 added: deep-linking to a specific record
                        across a cross-vehicle navigation (lands on the right tab, not the specific
                        record), and global Part-catalog-only matches (no navigation target without
                        a purchase or a catalog browse screen, the latter already deferred).
Open decisions:      None blocking. Standing instruction: verify and approve each phase before the
                     next one starts.
Do not:              Start Phase 12 or beyond without the user's go-ahead. Do not re-litigate or
                     re-surface items already tracked in DEFERRED.md as if they were forgotten. Do not
                     assume SQLite is available anywhere in this codebase. Do not assume a fresh
                     vehicle/user has any tabs visible beyond Dashboard - VisibleTabs defaults to
                     [Dashboard] only. Do not add a "MOT"/"Part"/etc. (any non-record-type) value to
                     the ImportMode enum - use a dedicated enum or a plain string for
                     provenance/category/result-kind concepts instead (OdometerRecordSource,
                     DocumentType, SearchResult.RecordType are all deliberately NOT ImportMode).
                     When any controller does a "move files from temp"/reconstruct-UploadedFiles
                     step, it MUST explicitly copy every field it wants to keep - grep for
                     "MoveFileFromTemp" across Controllers/Vehicle/ before adding any new
                     UploadedFiles field. Any enum embedded directly in a type used as a JSON
                     request-body wire format (not routed through a string-typed *ExportModel) needs
                     its own JsonStringEnumConverter. Before changing a shared model's field type
                     (like SearchResult.RecordType this phase), grep for every construction site
                     across the whole codebase, not just the method you're actively editing - a
                     second, unrelated feature (SearchRecordsByTags/vehicle image-map search) also
                     built SearchResult objects and broke on the same change. When calling record-add
                     API endpoints for testing, field names/casing are inconsistent across export
                     models and dates must match the server's locale (dd/mm/yyyy here) - check the
                     relevant *ExportModel class in Models/Shared/ImportModel.cs first. Note records
                     require both Description AND NoteText (not just Description) to save via the
                     API. Part is NOT vehicle-scoped (global catalog) but PartPurchase IS (VehicleId,
                     0=shop-wide). PartPurchase.QuantityRemaining must be set explicitly by the
                     caller, never by ToPartPurchase(). PlanRecord.ActualCost is preferred over Cost
                     (estimate) by the completion-conversion logic when non-zero, for all 5 target
                     record types. Government data is looked up by Vehicle.LicensePlate, never
                     VehicleIdentifier. OdometerRecord.Source must be preserved (not reset to Manual)
                     on manual edits of auto-inserted records. The root/dev user's config
                     (EnableAuth=false) reads directly from data/config/userConfig.json
                     (reloadOnChange) but is also cached in-memory per user for up to 1 hour -
                     restart the app after editing it to test reliably.
Last validation:     dotnet build (0 errors); on two throwaway vehicles (created and deleted via API,
                     real vehicle id 1 confirmed untouched throughout): a distinctive keyword on
                     vehicle A's ServiceRecord and vehicle B's NoteRecord both surfaced via
                     SearchRecordsAcrossVehicles with correct VehicleName/VehicleId/RecordType per
                     result; a Make/Model-only keyword returned a "Vehicle"-typed result; a Part's
                     number, reachable only via its PartPurchase, returned a correctly-labelled
                     "PartPurchase" result; the existing single-vehicle SearchRecords endpoint's
                     response was unchanged for the same keyword; rendered vehicle page HTML confirmed
                     the new "Search All Vehicles" toggle renders — 2026-08-17.
Last commit:         5151bc5 — "Phase 11: cross-vehicle global search" — user confirmed and approved
                     moving to Phase 12, 2026-08-17.
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
