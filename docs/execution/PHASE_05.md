# PHASE_05 — Parts Domain

## Design decision (confirmed with user before implementation)

Per `docs/REQUIREMENTS.md` FR-PART-01/02/03, `SupplyRecord` conflates "the part" with "a specific
purchase lot of that part" — cost/quantity live on the same row as the part definition, so the same
part bought twice at different prices can't be represented, and there's no reusable part catalog
independent of a single purchase.

Agreed design: split into two new, additive entities rather than modifying `SupplyRecord` (which
keeps working completely unchanged - no regression risk, no migration, per `CLAUDE.md`'s
"preserve until replacement has parity"):

- **`Part`** (`Models/Part/Part.cs`) - a reusable catalog entry. **Not** vehicle-scoped (a part
  exists once regardless of which vehicles use it). PartNumber, Manufacturer, Description,
  Category, Notes, Tags, ExtraFields, Files. No cost, no quantity.
- **`PartPurchase`** (`Models/Part/PartPurchase.cs`) - a transaction. Vehicle-scoped like
  `SupplyRecord` (`VehicleId == 0` = shop-wide, same existing convention). References a `Part` by
  `PartId`. Owns `Cost` (set once, immutable after creation), `Quantity`/`QuantityRemaining`
  (mutated by consumption/restoration - same mechanism `SupplyRecord` already uses, just not wired
  to any consuming record type yet), `Supplier`, `Notes`, `Files`, `Tags`, `ExtraFields`, and the
  same `RequisitionHistory: List<SupplyUsageHistory>` ledger type `SupplyRecord` already uses.

This means the same `Part` can have many `PartPurchase`s across different vehicles and times, each
keeping its own historical price - directly solving the "price belongs to a purchase, not the part"
requirement.

User chose to scope the first increment to **backend + API only**, verified via curl, with UI as a
separate follow-up increment - keeping this pass reviewable and consistent with the session's
established workflow.

## Task packet

```
TASK ID: PHASE-05-01
TITLE: Part/PartPurchase backend and API
OBJECTIVE: Stand up the new Parts domain as additive, verified-via-API entities, following the
  codebase's existing dual-backend (LiteDB/Postgres) data-access pattern exactly.
INPUTS: External/Interfaces/ISupplyRecordDataAccess.cs and its two implementations (vehicle-scoped
  template), External/Interfaces/IExtraFieldDataAccess.cs and IUserRecordDataAccess.cs
  (non-vehicle-scoped templates), Controllers/API/EquipmentController.cs (CRUD API template),
  Controllers/APIController.cs (main partial-class DI constructor), Program.cs (DI registration).
ALLOWED SCOPE: New Part/PartPurchase models; new data-access interfaces + LiteDB/Postgres
  implementations; new API export models; new Controllers/API/PartController.cs; DI wiring. No
  changes to SupplyRecord, no UI, no changes to any existing consuming record type (Service/Gas/
  Plan don't consume from Parts yet).
NON-SCOPE: UI (Vehicle/Parts tab, add/edit modals - follow-up increment); wiring Part/PartPurchase
  into the ImportMode/ExtraFields/Documents-aggregator/CSV-import machinery (deferred - would need
  its own ImportMode value(s) and has its own ripple effects, not required for CRUD to work);
  actual consumption/restoration logic against QuantityRemaining and RequisitionHistory (deferred -
  needs Service/Gas/Plan controllers to gain a "consume from PartPurchase" option, analogous to
  today's SupplyUsage, which is real additional scope); fitment/engine associations beyond the
  existing VehicleId=0 shop-wide convention.
IMPLEMENTATION REQUIREMENTS:
  - Part: global catalog entity, IPartDataAccess (GetParts/GetPartById/SavePart/DeletePartById),
    LiteDB + Postgres implementations mirroring the ExtraField/UserRecord non-vehicle-scoped
    pattern (JSONB blob storage, auto-increment id, no vehicleId column).
  - PartPurchase: vehicle-scoped transaction entity, IPartPurchaseDataAccess
    (GetPartPurchasesByVehicleId/GetPartPurchasesByPartId/GetPartPurchaseById/
    SavePartPurchaseToVehicle/DeletePartPurchaseById/DeleteAllPartPurchasesByVehicleId), LiteDB +
    Postgres implementations mirroring SupplyRecordDataAccess exactly, with an added partId column
    in Postgres to support the by-part price-history query.
  - API export models (PartExportModel, PartPurchaseExportModel) in Models/Shared/ImportModel.cs,
    following the existing string+JsonConverter convention used by every other *ExportModel.
  - Controllers/API/PartController.cs: full CRUD for both entities, matching existing filter/auth
    conventions exactly (CollaboratorFilter for vehicle-scoped PartPurchase actions, APIKeyFilter
    for API-key-authenticated writes, OperationResponse envelope, WebHookPayload.Generic event
    publishing) - Part itself only needs class-level [Authorize] since it has no vehicle context to
    check permissions against.
  - Register both new data-access pairs in Program.cs's existing LiteDB/Postgres DI branches.
DELIVERABLES: Working /api/parts/* and /api/vehicle/partpurchases/* endpoints.
ACCEPTANCE CRITERIA:
  - Full CRUD works for Part (add/get/list/update/delete) via curl.
  - Full CRUD works for PartPurchase, both vehicle-scoped and shop-wide (VehicleId=0).
  - The same Part can have multiple PartPurchases at different Cost values, and a price-history
    query (GetPartPurchasesByPartId) returns all of them correctly.
  - Existing SupplyRecord functionality is completely unaffected (nothing here touches it).
  - Server starts cleanly (confirms both new Postgres-style table-init blocks don't error even
    though this dev environment runs LiteDB, not Postgres, by default) and /health stays green.
VALIDATION COMMANDS:
  dotnet build (0 errors)
  dotnet run, then via curl: add a Part; add two PartPurchases for it at different vehicles/prices
  (one VehicleId>0, one VehicleId=0); list purchases by vehicle; list price history by part
  (confirm both purchases with their distinct costs appear); update the Part; delete both
  purchases then the Part; confirm /health stays green throughout.
STOP CONDITION: Acceptance criteria met, verified end-to-end via API, changes committed. UI is an
  explicitly separate follow-up increment, not part of this task's done-ness.
```

## What was done

1. Read the exact existing data-access conventions before writing anything: vehicle-scoped
   (`SupplyRecordDataAccess`) and non-vehicle-scoped (`ExtraFieldDataAccess`, `UserRecordDataAccess`)
   templates, and the `EquipmentController.cs` API CRUD template, to mirror precisely rather than
   inventing a new style.
2. Added `Part` and `PartPurchase` models, `IPartDataAccess`/`IPartPurchaseDataAccess` interfaces,
   and LiteDB + Postgres implementations for both (6 new files), registered in `Program.cs`'s
   existing DI branches.
3. Added `PartExportModel`/`PartPurchaseExportModel` to `Models/Shared/ImportModel.cs` alongside
   every other `*ExportModel`, and a new `Controllers/API/PartController.cs` with full CRUD for
   both entities, reusing `WebHookPayload.Generic` for event publishing (checked the actual
   available factory methods first rather than guessing a signature).
4. Extended the main `APIController.cs` partial-class constructor with the two new data-access
   dependencies (required for the new partial file's fields to resolve).
5. Verified end-to-end via curl against the running app: created a Part, purchased it twice (once
   for the user's real vehicle at one price, once shop-wide at a different price), confirmed the
   price-history endpoint correctly returns both purchases with their distinct costs - directly
   validating the core design goal. Verified update, delete, and that a locale-specific date-parsing
   "error" during testing was actually just a wrong test-input format (UK `dd/mm/yyyy`, matching
   the exact same `DateTime.Parse` behavior every other date-accepting endpoint in this codebase
   already has - not a bug). Confirmed `/health` stayed green and the user's real vehicle was
   untouched throughout.
6. Build check: 0 errors; 15 new nullable-reference warnings, all the same `CS8600`/`CS8603`/
   `CS8604` pattern already present in every other Postgres data-access file in the codebase - not
   a new category of issue.

## Deferred (documented, not forgotten)

- **UI** - no Parts tab/screen yet. Explicit user decision to keep this increment backend+API only.
- **ImportMode/ExtraFields/Documents-aggregator/CSV-import integration** - Part/PartPurchase don't
  have an `ImportMode` value, so they don't yet show up in the Phase 4 Documents tab, custom-field
  UI, or CSV import/export. Adding `ImportMode` values is cheap but has ripple effects across
  several switch statements and aggregators; deferred until the UI increment actually needs it.
- **Consumption/restoration wiring** - `PartPurchase.QuantityRemaining` and `RequisitionHistory`
  exist on the model but nothing decrements/restores them yet. Making Service/Gas/Plan records able
  to consume from a `PartPurchase` (mirroring today's `SupplyUsage` mechanism) is real additional
  scope, not attempted here.
- **Fitment/engine associations** beyond the existing shop-wide (`VehicleId=0`) convention -
  FR-PART-03's fuller vision (a part explicitly associated with specific engines/fitments
  independent of any one vehicle) isn't modeled yet; the current shop-wide convention is a
  reasonable interim per the original `SupplyRecord` precedent.

## Result

Backend and API complete, verified end-to-end via curl, not yet reviewed live by the user (no UI
exists yet to review). Next natural increment is the UI, pending user direction.
