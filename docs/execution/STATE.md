# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 13 — AI/OCR (confirmed deferred, no implementation)
Current task:       None - PHASE_13.md records the explicit decision not to build anything here,
                     per CLAUDE.md's locked "AI/OCR: deferred" rule and REQUIREMENTS.md
                     NFR-NONGOAL-01. Verified via grep that no AI/OCR code exists anywhere.
Status:             Complete by design (nothing implemented, matching the locked decision). Phase 12
                     (Local Reliability) is also complete and committed - see below.
Last completed:      Phase 11 finished (cross-vehicle global search, see PHASE_11.md), user confirmed
                     and approved moving to Phase 12. Phase 12: audited MakeBackup/RestoreBackup
                     (confirmed whole-DB-file-copy already covers Part/PartPurchase, no code change
                     needed there) then traced where that data actually gets *used* elsewhere and
                     found two real gaps in code that predates Phase 5's Part/PartPurchase addition:
                     VehicleLogic.GetVehicleDocuments (feeds /api/cleanup's unlinked-file deletion)
                     never included PartPurchase.Files, and DeleteVehicleRecords never called the
                     already-existing (but never-wired-up) DeleteAllPartPurchasesByVehicleId. Fixed
                     both, added GetPartDocuments (mirroring the existing GetStoreSupplyDocuments
                     pattern), and added a new GetBrokenAttachmentLinks diagnostic (the reverse of
                     the existing ClearUnlinkedDocuments - reports, never auto-deletes, DB records
                     pointing at missing files) surfaced in the same /api/cleanup response.
Next task:           Phase 14 (V1 Hardening) is next and is explicitly open-ended per its roadmap
                     description (tests, accessibility, responsive/mobile validation, performance,
                     security review, error handling). Ask the user for scope/priorities before
                     starting rather than guessing at an unbounded phase.
Known blockers:      1. No browser/screenshot tool in this environment - Phase 12 is entirely a
                        backend/data-integrity phase, fully curl-verifiable, no UI surface to review
                        live beyond the JSON response shape shown to root users on the Admin cleanup
                        action (not a high-value live-browser check this time).
                     2. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     3. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. Phase 12 added: broader DB-corruption detection
                        (health check stays connectivity-only per NFR-REL-02), and Postgres backend
                        backup coverage (pre-existing gap, not introduced by Car Tracker, out of this
                        phase's explicitly-named scope).
Open decisions:      None blocking. Standing instruction: verify and approve each phase before the
                     next one starts. Phase 13 (AI/OCR) is explicitly a non-implementation phase per
                     CLAUDE.md's locked decision - "complete" it by confirming nothing was built, not
                     by building anything.
Do not:              Start Phase 13 or beyond without the user's go-ahead. Do not implement any
                     AI/OCR feature work in Phase 13 - CLAUDE.md locks this as deferred; the phase is
                     "complete" once confirmed as untouched, this is not something to build around or
                     stub out speculatively either. Do not re-litigate or re-surface items already
                     tracked in DEFERRED.md as if they were forgotten. Do not assume SQLite is
                     available anywhere in this codebase. Do not assume a fresh vehicle/user has any
                     tabs visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only. Do not
                     add a "MOT"/"Part"/etc. (any non-record-type) value to the ImportMode enum - use
                     a dedicated enum or a plain string instead. When any controller does a "move
                     files from temp"/reconstruct-UploadedFiles step, it MUST explicitly copy every
                     field it wants to keep. When adding a new entity type with its own Files/
                     attachments (like Part/PartPurchase was in Phase 5), grep for
                     GetVehicleDocuments/DeleteVehicleRecords/ClearUnlinkedDocuments in
                     Logic/VehicleLogic.cs and wire the new type into all three - this exact class of
                     "new entity type silently missing from the cleanup/cascade-delete/backup-usage
                     mechanisms written before it existed" bug has now happened once for real; the
                     pattern is worth checking proactively for any future new persisted entity type.
                     Any enum embedded directly in a type used as a JSON request-body wire format
                     needs its own JsonStringEnumConverter. When calling record-add API endpoints for
                     testing, field names/casing are inconsistent across export models and dates must
                     match the server's locale (dd/mm/yyyy here). Note records require both
                     Description AND NoteText. Some fields (e.g. Vehicle.HasOdometerAdjustment) are
                     MVC-only, not exposed on the API's *ImportModel DTOs. Part is NOT vehicle-scoped
                     (global catalog) but PartPurchase IS (VehicleId, 0=shop-wide).
                     PartPurchase.QuantityRemaining must be set explicitly by the caller, never by
                     ToPartPurchase(). PlanRecord.ActualCost is preferred over Cost (estimate) by the
                     completion-conversion logic when non-zero. Government data is looked up by
                     Vehicle.LicensePlate, never VehicleIdentifier. OdometerRecord.Source must be
                     preserved (not reset to Manual) on manual edits of auto-inserted records. The
                     root/dev user's config (EnableAuth=false) reads directly from
                     data/config/userConfig.json but is cached in-memory for up to 1 hour - restart
                     the app after editing it. curl's `-d "key=/path/with/slashes"` can silently fail
                     to bind a form field in ways `--data-urlencode` or a query-string param won't -
                     if a POST action mysteriously returns a false/empty early-return result for a
                     path-like parameter, try the query-string form before assuming an app bug.
Last validation:     dotnet build (0 errors); on throwaway vehicles/parts (created and deleted via
                     API, real vehicle id 1 confirmed untouched throughout): deep-clean preserved a
                     PartPurchase's real attachment file after the fix (0 deleted; before the fix this
                     would have deleted it); manually removing that same file from disk was correctly
                     flagged by broken_attachment_links_found=1 without touching the DB record;
                     deleting a vehicle with a PartPurchase now correctly removes the PartPurchase too
                     (confirmed via direct query, not just absence from the UI); a full backup - delete
                     vehicle - restore backup round trip brought the vehicle back exactly as it was —
                     2026-08-17.
Last commit:         f6b49d2 — "Record Phase 12 commit hash in STATE.md". Phase 13's PHASE_13.md/
                     ROADMAP.md updates not yet committed - pending with this STATE.md update.
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
