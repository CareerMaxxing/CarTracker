# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 10 — Documents
Current task:       PHASE-10-01 (see docs/execution/PHASE_10.md) — document type categorization
Status:             Complete. Curl-verified end-to-end on a throwaway vehicle (deleted after): Type
                     set via JSON API and via the actual MVC save path both round-trip correctly, a
                     systemic bug (Type silently dropped by every controller's temp-file-move step)
                     caught and fixed before shipping, Documents tab category column/filter pills
                     confirmed rendering real data. Real vehicle (id 1) confirmed untouched. Not yet
                     shown to the user live in browser - offering that before Phase 11.
Last completed:      Phase 9 finished (odometer Source provenance + regression flagging, see
                     PHASE_09.md), user confirmed and approved moving to Phase 10. Phase 10: added
                     DocumentType enum (Other=0/Invoice/MOT/V5C/Insurance/Photograph/Datasheet) and
                     UploadedFiles.Type; extended the existing editFileName rename dialog (shared.js)
                     with a Type dropdown instead of touching all 14 record modals individually;
                     added a category badge/icon to the two shared file-list partials every modal
                     already includes; added a Category column + filter pills to the Phase 4
                     Documents tab (renamed its pre-existing "Type" column to "Record Type" to avoid
                     colliding with the new, differently-scoped concept). Caught two real bugs before
                     shipping: (1) UploadedFiles is used directly as the JSON wire type for Files in
                     most record DTOs, and System.Text.Json doesn't accept a named string for a plain
                     enum by default - fixed with a JsonStringEnumConverter scoped to just that one
                     property; (2) far more serious - all 13 controllers' Save*ToVehicleId actions
                     have a "move files from temp" step that reconstructs each UploadedFiles object
                     copying only Name/Location, silently dropping Type (and anything else not
                     explicitly listed) on every single save, not just edits - this would have made
                     the whole feature non-functional through the real browser UI despite working
                     over raw API calls. Fixed all 15 occurrences across 13 files.
Next task:           Show the user the rename dialog's new Type dropdown and the Documents tab's
                     category filter, confirm it looks right, then ask before starting Phase 11
                     (Global Search) or anything else.
Known blockers:      1. No browser/screenshot tool in this environment - Phase 10's changes are
                        curl-verifiable for correctness (JSON round-trip, rendered HTML) but the
                        rename-dialog dropdown and filter-pill interaction are genuinely worth a live
                        look.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01) -
                        this is literally the next phase now, must be resolved before implementing.
                     3. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. Phase 10 added: bulk re-categorization tooling,
                        and a dedicated document-management screen beyond the existing Documents tab.
Open decisions:      None blocking. Standing instruction: verify and approve each phase before the
                     next one starts.
Do not:              Start Phase 11 or beyond without the user's go-ahead. Do not re-litigate or
                     re-surface items already tracked in DEFERRED.md as if they were forgotten. Do not
                     assume SQLite is available anywhere in this codebase. Do not assume a fresh
                     vehicle/user has any tabs visible beyond Dashboard - VisibleTabs defaults to
                     [Dashboard] only. Do not add a "MOT" (or any non-record-type) value to the
                     ImportMode enum - use a dedicated enum for provenance/category-only concepts
                     instead (OdometerRecordSource, DocumentType). When any controller does a
                     "move files from temp"/reconstruct-UploadedFiles step, it MUST explicitly copy
                     every field it wants to keep (Type included) - the reconstruction pattern used
                     everywhere only copies what's explicitly listed, silently dropping the rest; this
                     class of bug will recur if a new field is ever added to UploadedFiles again
                     without grepping for "MoveFileFromTemp" across Controllers/Vehicle/ first. Any
                     new enum embedded directly in UploadedFiles (or any type used directly as a JSON
                     request-body wire format, not routed through a string-typed *ExportModel) needs
                     its own JsonStringEnumConverter or it will fail to deserialize named string
                     values from JSON bodies. When calling record-add API endpoints for testing, field
                     names/casing are inconsistent across export models and dates must match the
                     server's locale (dd/mm/yyyy here) - check the relevant *ExportModel class in
                     Models/Shared/ImportModel.cs first rather than guessing. Some fields (e.g.
                     Vehicle.HasOdometerAdjustment) are MVC-only and not exposed on the API's
                     *ImportModel DTOs at all. Part is NOT vehicle-scoped (global catalog) but
                     PartPurchase IS (VehicleId, 0=shop-wide). PartPurchase.QuantityRemaining must be
                     set explicitly by the caller, never by ToPartPurchase(). PlanRecord.ActualCost is
                     preferred over Cost (estimate) by the completion-conversion logic when non-zero,
                     for all 5 target record types. Government data is looked up by
                     Vehicle.LicensePlate, never VehicleIdentifier. OdometerRecord.Source must be
                     preserved (not reset to Manual) on manual edits of auto-inserted records. The
                     root/dev user's config (EnableAuth=false) reads directly from
                     data/config/userConfig.json (reloadOnChange) but is also cached in-memory per
                     user for up to 1 hour - restart the app after editing it to test reliably.
Last validation:     dotnet build (0 errors); on a throwaway vehicle (created and deleted via API,
                     real vehicle id 1 confirmed untouched throughout): JSON API add with an explicit
                     Files[].Type round-tripped correctly after the JsonStringEnumConverter fix; a
                     file with no Type defaulted to Other; form-encoded save through the actual MVC
                     SaveOdometerRecordToVehicleId action initially silently reset Type to Other
                     (reproducing the systemic bug), then correctly preserved Datasheet after the fix
                     was applied across all 13 controllers; rendered Documents tab HTML confirmed the
                     Category column, filter pills, and data-tags all reflect real per-file types
                     across multiple record types — 2026-08-17.
Last commit:         299cd0e — "Phase 10: document type categorization" — user confirmed and
                     approved moving to Phase 11, 2026-08-17.
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
