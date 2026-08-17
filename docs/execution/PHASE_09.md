# PHASE_09 — Mileage / Odometer

## Scope

Per `REQUIREMENTS.md` FR-ODO-01/02 (Phase 9): odometer readings must record their provenance, and
the system should flag suspicious mileage regressions rather than silently accepting them.

## Task packet

```
TASK ID: PHASE-09-01
TITLE: Odometer reading provenance + regression flagging
OBJECTIVE: Give every OdometerRecord a Source (who/what generated this reading), correctly set at
  every existing auto-insert call site, and flag (not block) a manually-entered mileage that's
  lower than the vehicle's last reported reading unless HasOdometerAdjustment is set.
INPUTS: Models/OdometerRecord/OdometerRecord.cs, Logic/OdometerLogic.cs, all 16 call sites of
  AutoInsertOdometerRecord (API + Vehicle MVC controllers for Gas/Service/Repair/Upgrade,
  InspectionController, PlanController, ImportController's CSV bulk import, VehicleController's
  BulkCreateOdometerRecords), Controllers/Vehicle/OdometerController.cs (manual entry form).
ALLOWED SCOPE: New OdometerRecordSource enum; Source field on OdometerRecord; setting Source
  correctly at every existing auto-insert/import call site; a regression-check method in
  IOdometerLogic; wiring it into the manual entry form's save action with a non-blocking warning
  surfaced to the user; exposing Source read-only via the API export model; a small provenance icon
  in the Odometer tab's list view.
NON-SCOPE: Applying the regression check to the ~15 auto-insert call sites (Gas/Service/Repair/
  Upgrade/Inspection/Plan forms) - the acceptance criterion specifically concerns "entering a
  mileage value" on the dedicated Odometer entry form; extending those other forms is real
  additional UX/response-shape work across 8+ controllers and their JS, deferred rather than
  bundled in. CSV import/export column wiring for Source (consistent with Phase 6's ActualCost
  precedent - deferred there too). Making Source settable via the API's Add/Update odometer
  endpoints (deliberately read-only/system-determined, matching Phase 8's IsMockData precedent).
IMPLEMENTATION REQUIREMENTS:
  - OdometerRecordSource: Manual(0)/ServiceRecord/RepairRecord/GasRecord/UpgradeRecord/TaxRecord/
    InspectionRecord/MOT/Other. Deliberately separate from ImportMode (which drives CSV import/tabs/
    VisibleTabs and shouldn't gain a value like MOT that has no record type of its own). Manual=0 so
    pre-existing records without this field (added before Phase 9) deserialize to a safe default.
  - Set Source at all 16 call sites, matching the record type that triggered the insert (a
    StaticHelper.ToOdometerRecordSource(ImportMode) mapping helper for the 2 call sites where the
    triggering type is a runtime variable - PlanController's completion path and VehicleController's
    BulkCreateOdometerRecords - literal enum values everywhere else, since the type is already known
    at each call site).
  - IOdometerLogic.IsSuspiciousMileageRegression(vehicleId, newMileage, excludeRecordId): returns
    false if the vehicle has HasOdometerAdjustment set (intentional replacement/rollback, not a
    mistake); otherwise compares against GetLastOdometerRecordMileage (max mileage on record,
    reusing the existing method/definition rather than inventing a new "most recent by date" notion).
  - SaveOdometerRecordToVehicleId (manual add/edit): checks regression before saving, but still
    saves either way (flag, don't block, since the user may be intentionally backfilling an older
    record); returns AdditionalData.isSuspiciousRegression=true on the flagged case, consumed by
    odometerrecord.js as a warnToast instead of the normal successToast.
DELIVERABLES: OdometerRecordSource enum, Source field + correct tagging at every call site,
  regression-flagging on manual entry, Source exposed read-only via the API, Source icon in the UI.
ACCEPTANCE CRITERIA:
  - Every new OdometerRecord (auto-inserted or manual) has a non-default-in-error Source value.
  - Adding a Service record (with auto-odometer-insert on) produces an OdometerRecord with
    Source=ServiceRecord, retrievable via GET.
  - Manually entering a mileage lower than the vehicle's last reported mileage produces
    isSuspiciousRegression=true in the response, but the record still saves.
  - The same entry with HasOdometerAdjustment set on the vehicle does not flag.
  - Editing an existing auto-inserted record via the manual form does not reset its Source back to
    Manual.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against a throwaway vehicle (deleted after) - temporarily flipped
  EnableAutoOdometerInsert in data/config/userConfig.json (backed up first, restored byte-identical
  after, app restarted before and after so the file-based config change and its reversal both take
  effect cleanly): added a Service record and confirmed the resulting odometer record's Source;
  manually added a lower-mileage record and confirmed the warning; set HasOdometerAdjustment on the
  vehicle via the MVC SaveVehicle endpoint (the API's VehicleImportModel doesn't expose this field)
  and confirmed the warning stopped firing; edited the auto-inserted record via the manual form and
  confirmed its Source was preserved, not reset; fetched the rendered Odometer tab HTML and
  confirmed the provenance icon appears with the correct tooltip.
STOP CONDITION: Acceptance criteria met, verified via curl, real vehicle (id 1) confirmed untouched
  throughout, changes committed.
```

## What was done

1. Read `REQUIREMENTS.md` FR-ODO-01/02 and audited every call site of `AutoInsertOdometerRecord`
   (16 total, across 10 files) plus the manual entry form and the CSV-direct-odometer-import branch,
   to understand exactly what provenance information was actually available at each site before
   designing the enum.
2. Considered and rejected reusing the existing `ImportMode` enum for Source, despite several call
   sites already tagging attachments with `ImportMode.X` right next to the auto-insert call (e.g.
   `StaticHelper.CreateAttachmentFromRecord(ImportMode.ServiceRecord, ...)`) - `ImportMode` drives
   CSV import type selection, tab visibility (`VisibleTabs`), and several other switch statements
   across the app; adding a `MOT` value to it (needed per FR-ODO-01's own wording) would incorrectly
   make MOT appear as a selectable CSV import type / tab, since it isn't a record type with its own
   CRUD. Created a dedicated `OdometerRecordSource` enum instead.
3. Added `OdometerRecord.Source` (defaults to `Manual`, so old records without the field, and
   genuine manual entries, both read correctly with no migration needed).
4. Set `Source` correctly at all 16 call sites: literal enum values where the triggering record type
   was already known at that point in the code (14 sites), and a new
   `StaticHelper.ToOdometerRecordSource(ImportMode)` mapping helper for the 2 sites where it's a
   runtime variable (`PlanController`'s plan-completion conversion, which already used
   `existingRecord.ImportMode` for attachment tagging; `VehicleController.BulkCreateOdometerRecords`,
   which already used the `importMode` switch variable for the same purpose).
5. Self-caught and fixed a bug before it shipped, same class as Phase 5's `QuantityRemaining` issue:
   `SaveOdometerRecordToVehicleId` (manual edit form) reconstructs a fresh `OdometerRecord` from the
   input and `Upsert`s it (full-row replace) - since the input DTO has no `Source` field, editing an
   auto-inserted record through the manual edit modal would have silently reset its `Source` back to
   `Manual` on every edit. Fixed by fetching the existing record first when `Id != default` and
   carrying its `Source` forward before saving.
6. Added `IOdometerLogic.IsSuspiciousMileageRegression(vehicleId, newMileage, excludeRecordId)`:
   exempts vehicles with `HasOdometerAdjustment` set (an intentional odometer replacement/rollback,
   not a mistake), otherwise compares the new mileage against `GetLastOdometerRecordMileage` (the
   existing max-mileage definition, reused rather than inventing a new "most recent by date" notion
   that would flag legitimate historical backfilling more aggressively than intended).
7. Wired the check into `SaveOdometerRecordToVehicleId`: flags but does not block the save (a user
   backfilling an older record is a legitimate case FR-ODO-02 explicitly distinguishes from a
   mistake); returns `isSuspiciousRegression: true` in `AdditionalData` on the flagged case.
8. Updated `odometerrecord.js`'s save handler to show `warnToast(...)` instead of the normal
   `successToast(...)` when the response carries that flag, using the existing (previously unused
   outside one wake-lock message) `warnToast` helper in `shared.js`.
9. Exposed `Source` read-only on `OdometerRecordExportModel` (API GET/list only, not settable via
   Add/Update - system-determined provenance shouldn't be spoofable by an API caller, matching Phase
   8's `IsMockData` precedent for "the API can tell you this, but not set it").
10. Added a small provenance icon (`bi-arrow-repeat`, tooltip "Auto-inserted from: X") to the
    Odometer tab's list view for non-Manual records, without building out the full
    visible-columns/CSV-export machinery used by the tab's other columns (that's meaningfully more
    scope than a single field needs - noted as a nice-to-have in `DEFERRED.md` instead).
11. Verified via curl against a throwaway vehicle (created and deleted after) on a running instance:
    - Temporarily flipped `EnableAutoOdometerInsert` in `data/config/userConfig.json` (backed up
      first; the app's root-user config path reads this file directly rather than per-user DB
      storage, confirmed by reading `ConfigHelper.GetUserConfig`), restarted the app so the change
      and its later reversal both took effect (the setting is cached in-memory per request-user for
      up to an hour otherwise), added a Service record, and confirmed the resulting `OdometerRecord`
      had `Source: "ServiceRecord"`.
    - Manually added a lower-mileage record and confirmed `isSuspiciousRegression: true`; added a
      higher-mileage record and confirmed no warning.
    - Set `HasOdometerAdjustment` on the vehicle (discovered along the way that the API's
      `VehicleImportModel` doesn't expose this field at all - used the MVC `SaveVehicle` endpoint
      instead) and confirmed a subsequent lower-mileage entry no longer warned.
    - Edited the Service-sourced record via the manual edit form and confirmed `Source` stayed
      `ServiceRecord`, not reset to `Manual`.
    - Fetched the rendered Odometer tab HTML and confirmed the provenance icon and tooltip render
      correctly.
    - Deleted the throwaway vehicle, restored `userConfig.json` to a byte-identical copy of its
      original content, restarted the app, and confirmed the real vehicle (id 1, the BMW) and its
      (empty) odometer history were untouched throughout.
    - `dotnet build`: 0 errors throughout.

## Result

Complete. `REQUIREMENTS.md` FR-ODO-01/02 satisfied: every odometer reading now carries a correct
provenance value, and manual entry warns (without blocking) on suspicious regressions unless the
vehicle's odometer was intentionally adjusted.
