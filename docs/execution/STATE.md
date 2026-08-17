# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 9 — Mileage / Odometer
Current task:       PHASE-09-01 (see docs/execution/PHASE_09.md) — Source provenance + regression
                     flagging
Status:             Complete. Curl-verified end-to-end on a throwaway vehicle (deleted after): Source
                     tagging on auto-insert, regression warning on manual entry, HasOdometerAdjustment
                     exemption, edit-preserves-Source bug caught and fixed before shipping. Real
                     vehicle (id 1) and its config confirmed untouched. Not yet shown to the user live
                     in browser - offering that before Phase 10, per the standing phase-by-phase
                     check-in instruction.
Last completed:      Phase 8 finished (mocked DVLA/DVSA adapters + Dashboard panel, see PHASE_08.md),
                     user confirmed and approved moving to Phase 9. Phase 9: added OdometerRecordSource
                     enum (deliberately separate from ImportMode - see Known blockers/Do-not below),
                     set Source correctly at all 16 AutoInsertOdometerRecord call sites plus the
                     manual entry form, added IOdometerLogic.IsSuspiciousMileageRegression (exempts
                     HasOdometerAdjustment vehicles), wired a non-blocking warning into the manual
                     Odometer tab's save action, exposed Source read-only via the API export model,
                     added a small provenance icon in the Odometer tab list. Self-caught and fixed one
                     bug before shipping: the manual edit form reconstructs a fresh OdometerRecord and
                     Upserts it (full-row replace) - editing an auto-inserted record would have
                     silently reset its Source back to Manual every time; fixed by fetching and
                     carrying forward the existing record's Source on edit.
Next task:           Show the user the Odometer tab (provenance icon + a live regression-warning
                     demo) and confirm it looks right, then ask before starting Phase 10 (Documents)
                     or anything else - per the standing phase-by-phase check-in instruction and
                     CLAUDE.md's phase-boundary rule.
Known blockers:      1. No browser/screenshot tool in this environment - continue the "review locally
                        as I go" workflow; Phase 9's changes are mostly curl-verifiable (JSON fields,
                        response flags) but the warnToast and provenance icon are genuinely visual -
                        worth a live look, unlike Phase 8's fully server-rendered panel.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. Phase 9 added: regression-flagging on the other
                        auto-insert forms (Gas/Service/Repair/Upgrade/Inspection/Plan), Source CSV
                        column, a full Source table column (icon-only for now), and MOT-sourced
                        odometer auto-insert (the enum value exists, nothing sets it yet - natural
                        follow-on now that Phase 8's DVSA mock returns per-test OdometerValue).
Open decisions:      None blocking. Standing instruction: verify and approve each phase before the
                     next one starts.
Do not:              Start Phase 10 or beyond without the user's go-ahead. Do not re-litigate or
                     re-surface items already tracked in DEFERRED.md as if they were forgotten -
                     they're intentionally parked. Do not assume SQLite is available anywhere in this
                     codebase. Do not assume a fresh vehicle/user has any tabs visible beyond
                     Dashboard - VisibleTabs defaults to [Dashboard] only; check/set it first or use
                     the API directly when testing a vehicle detail page. Do not add a "MOT" (or any
                     non-record-type) value to the ImportMode enum - it drives CSV import/tabs/
                     VisibleTabs; use OdometerRecordSource (or a similarly dedicated enum) for
                     provenance-only concepts instead. When calling record-add API endpoints for
                     testing, field names/casing are inconsistent across export models and dates must
                     match the server's locale (dd/mm/yyyy here) - check the relevant *ExportModel
                     class in Models/Shared/ImportModel.cs first rather than guessing. Some fields
                     (e.g. Vehicle.HasOdometerAdjustment) are MVC-only and not exposed on the API's
                     *ImportModel DTOs at all - check both the API and MVC input models before
                     assuming a field is settable via the API. Part is NOT vehicle-scoped (global
                     catalog) but PartPurchase IS (VehicleId, 0=shop-wide). PartPurchase.
                     QuantityRemaining must be set explicitly by the caller, never by ToPartPurchase().
                     PlanRecord.ActualCost is preferred over Cost (estimate) by the completion-
                     conversion logic when non-zero, for all 5 target record types. Government data is
                     looked up by Vehicle.LicensePlate, never VehicleIdentifier. OdometerRecord.Source
                     must be preserved (not reset to Manual) on manual edits of auto-inserted records -
                     see SaveOdometerRecordToVehicleId. The root/dev user's config (EnableAuth=false)
                     reads directly from data/config/userConfig.json (reloadOnChange) but is also
                     cached in-memory per user for up to 1 hour - a file edit alone isn't enough to
                     test a config-gated code path reliably; restart the app after editing it.
Last validation:     dotnet build (0 errors); on a throwaway vehicle (created and deleted via API,
                     real vehicle id 1 confirmed untouched throughout): Service-record add (with
                     EnableAutoOdometerInsert temporarily enabled in userConfig.json, backed up and
                     byte-identically restored after, app restarted before and after) produced an
                     odometer record with Source=ServiceRecord; manual lower-mileage entry returned
                     isSuspiciousRegression=true, higher-mileage entry did not; HasOdometerAdjustment
                     (set via MVC SaveVehicle, since the API's VehicleImportModel doesn't expose it)
                     suppressed the warning; editing the Service-sourced record via the manual form
                     preserved Source=ServiceRecord; rendered Odometer tab HTML confirmed showing the
                     provenance icon with correct tooltip — 2026-08-17.
Last commit:         04d6f8b — "Record Phase 8 commit hash in STATE.md" (Phase 9's commit not yet
                     made - pending user confirmation of this phase first).
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
