# DATA_MODEL.md

Baseline data model of LubeLogger as cloned, established during Phase 0 reconnaissance
(2026-08-17). Namespace for all models: `CarCareTracker.Models`. This describes what **exists
today**; Phase 1/5/6 will extend it for Parts and Planned Engineering Work rather than duplicating
it — see "Reuse opportunities" at the end.

## Persistence mechanics (see also ARCHITECTURE.md)

No ORM, no AutoMapper. Each entity has one `I*DataAccess` interface (`External/Interfaces/`) with
two backend implementations (`External/Implementations/{Litedb,Postgres}/`). LiteDB stores whole
objects as documents in a named collection; Postgres stores them as JSONB rows in `app.<table>`.
DTO↔domain conversion is hand-written `.ToXRecord()` methods on each `*Input` class (used for form
binding). IDs are backend auto-increment ints.

| Entity | Table/collection |
|---|---|
| Vehicle | `vehicles` |
| ServiceRecord | `servicerecords` |
| CollisionRecord (repair) | `collisionrecords` |
| UpgradeRecord | `upgraderecords` |
| GasRecord | `gasrecords` |
| TaxRecord | `taxrecords` |
| OdometerRecord | `odometerrecords` |
| ReminderRecord | `reminderrecords` |
| PlanRecord | `planrecords` |
| PlanRecordTemplate (stored as `PlanRecordInput`) | `planrecordtemplates` |
| Note | `notes` |
| SupplyRecord | `supplyrecords` |
| InspectionRecord | `inspectionrecords` |
| InspectionRecordTemplate (stored as `InspectionRecordInput`) | `inspectionrecordtemplates` |
| EquipmentRecord | `equipmentrecords` |
| ExtraField definitions (`RecordExtraField`) | `extrafields` |
| UserData | `userrecords` |
| UserConfigData | `userconfigrecords` |
| UserAccess | `useraccessrecords` |
| UserHousehold | `userhouseholdrecords` |
| APIKey | `apikeyrecords` |
| Token | `tokenrecords` |

## Core entities

### Vehicle (`Models/Vehicle/Vehicle.cs`)
`Id`, `ImageLocation`, `MapLocation`, `Year`, `Make`, `Model`, `LicensePlate`, `PurchaseDate`/
`SoldDate` (string, custom JSON converter), `PurchasePrice`/`SoldPrice` (decimal), `IsElectric`,
`IsDiesel`, `UseHours`, `OdometerOptional`, `ExtraFields: List<ExtraField>`, `Tags: List<string>`,
`HasOdometerAdjustment`, `OdometerMultiplier`, `OdometerDifference`, `DashboardMetrics`,
`VehicleIdentifier` (which field replaces "license plate" in the UI, e.g. VIN).

Related: `VehicleViewModel` (computed `LastReportedMileage`, `HasReminders`, `CostPerMile`,
`TotalCost`, `DistanceUnit`); `VehicleImageMap`/`ImageMap` (clickable-diagram overlay metadata).

### GenericRecord (base shape for Service/Collision/Upgrade records)
`Models/Shared/GenericRecord.cs`: `Id`, `VehicleId` (FK), `Date`, `Mileage`, `Description`,
`Cost` (**single scalar decimal**), `Notes`, `Files: List<UploadedFiles>`, `Tags: List<string>`,
`ExtraFields: List<ExtraField>`, `RequisitionHistory: List<SupplyUsageHistory>` (parts consumed on
this record).

Subclasses (empty bodies — pure type discrimination): `ServiceRecord`, `CollisionRecord` (aka
"repair" in UI), `UpgradeRecord`. Each has a parallel `*Input` DTO with `Date` as string, plus
`Supplies: List<SupplyUsage>` (parts to consume) and a `.ToXRecord()` mapper.
`ServiceRecordInput` additionally carries `ReminderRecordId: List<int>`, linking a completed
service back to the reminder(s) it satisfies.

### GasRecord (`Models/GasRecord/GasRecord.cs`)
`Id`, `VehicleId`, `Date`, `Mileage`, `Gallons`, `Cost`, `IsFillToFull`, `MissedFuelUp`, `Notes`,
`StartingSoc`/`EndingSoc` (EV state-of-charge), `Files`, `Tags`, `ExtraFields`,
`RequisitionHistory`.

### OdometerRecord (`Models/OdometerRecord/OdometerRecord.cs`)
`Id`, `VehicleId`, `Date`, `InitialMileage`, `Mileage`, computed `DistanceTraveled`, `Notes`,
`Tags`, `Files`, `ExtraFields`, `EquipmentRecordId: List<int>` (equipment "equipped" at the time of
reading — auto-populated by `OdometerLogic.AutoInsertOdometerRecord`).

### TaxRecord (`Models/TaxRecord/TaxRecord.cs`)
`Id`, `VehicleId`, `Date`, `Description`, `Cost`, `Notes`, `IsRecurring`,
`RecurringInterval: ReminderMonthInterval`, `CustomMonthInterval`, `CustomMonthIntervalUnit`,
`Files`, `Tags`, `ExtraFields`. Recurrence auto-rolls forward via `VehicleLogic.UpdateRecurringTaxes`.

### ReminderRecord — "upcoming service", concept #1 (`Models/Reminder/ReminderRecord.cs`)
`Id`, `VehicleId`, `Date`, `Mileage`, `Description`, `Notes`, `IsRecurring`, `UseCustomThresholds`,
`FixedIntervals`, `CustomThresholds: ReminderUrgencyConfig`, `CustomMileageInterval`,
`CustomMonthInterval`, `CustomMonthIntervalUnit`, `ReminderMileageInterval`,
`ReminderMonthInterval`, `Metric: ReminderMetric` (Date/Odometer/Both), `Tags`.

`ReminderRecordViewModel` adds computed `Urgency`, `UserMetric`, `DueDays`, `DueMileage` (built by
`Helper/ReminderHelper.cs` against `ReminderUrgencyConfig` thresholds). `ServiceRecord`/
`PlanRecord` both carry `ReminderRecordId(s)`, closing the loop from "reminder fired" to "work done".

### PlanRecord — "planned work", concept #2 (`Models/PlanRecord/PlanRecord.cs`)
**Closest existing analog to "Planned Engineering Work" in the target spec.**

`Id`, `VehicleId`, `ReminderRecordId` + `ReminderRecordIds: List<int>`, `DateCreated`,
`DateModified`, `Description`, `Notes`, `Files`, `ImportMode: ImportMode` (what record type this
becomes once done — ServiceRecord/RepairRecord/GasRecord/etc.), `Priority: PlanPriority`
(Critical/Normal/Low), `Progress: PlanProgress` (Backlog/InProgress/Testing/Done — Kanban),
`Cost` (single scalar, "estimated cost"), `ExtraFields`,
`RequisitionHistory: List<SupplyUsageHistory>` (parts already consumed against the plan).

`PlanRecordTemplate` reuses `PlanRecordInput` itself as its storage shape (no separate model
class) — a reusable-template pattern worth reusing for Planned Work templates in the new spec.

### Note (`Models/Note/Note.cs`)
`Id`, `VehicleId`, `Description`, `NoteText`, `Pinned`, `Tags`, `Files`, `ExtraFields`.

### InspectionRecord (`Models/InspectionRecord/*.cs`)
`Id`, `VehicleId`, `Date`, `Mileage`, `Cost`, `Description`, `Files`, `Tags`,
`Results: List<InspectionRecordResult>`, computed `Failed`. `InspectionRecordTemplateField`
(reusable checklist field) notably carries `ActionItemType: ImportMode`, `ActionItemDescription`,
`ActionItemPriority: PlanPriority`, `HasActionItem` — **a failed inspection field can auto-generate
a `PlanRecord`**, i.e. LubeLogger already has one instance of "inspection failure → planned work"
automation to look at before designing Car Tracker's own workflow automation.

### EquipmentRecord (`Models/EquipmentRecord/EquipmentRecord.cs`)
`Id`, `VehicleId`, `Description`, `IsEquipped`, `Notes`, `Tags`, `Files`, `ExtraFields`. Swappable
equipment (e.g. tire sets); "equipped" state is snapshotted onto `OdometerRecord.EquipmentRecordId`.

### SupplyRecord — "Parts", closest existing analog (`Models/Supply/SupplyRecord.cs`)
`Id`, `VehicleId` (`0` = shared "store" inventory), `Date` (purchase date), `PartNumber`,
`PartSupplier`, `Quantity`, `Description`, `Cost` (**cost of the whole purchased lot, not
itemized per unit**), `Notes`, `Files`, `Tags`, `ExtraFields`,
`RequisitionHistory: List<SupplyUsageHistory>`.

- `SupplyUsage { SupplyId, Quantity }` — a request to consume N units when saving a Service/Gas/
  Plan record.
- `SupplyUsageHistory { Id, Date, PartNumber, Description, Quantity, Cost }` — an immutable ledger
  line written to **both** the `SupplyRecord.RequisitionHistory` (inventory side) and the consuming
  record's own `RequisitionHistory`. This is the one place in the codebase with a quantity+cost
  pair distinct from a record's top-level `Cost` — but it's still embedded, not a first-class
  purchase/transaction row with its own identity. **`SupplyRecord` conflates "the part" with "a
  specific purchase lot of that part" — this is exactly the gap the target spec's "price belongs
  to a purchase/transaction, not the part" principle needs to fix.**

### Users / access (`Models/User/*.cs`)
- `UserData` — `Id`, `UserName`, `EmailAddress`, `Password` (hash, unsalted SHA-256), `IsAdmin`,
  `IsRootUser`.
- `UserConfigData` — `{ Id (=UserId), UserConfig }`.
- `UserAccess { Id: UserVehicle { UserId, VehicleId } }` — the join entity granting a user access
  to a vehicle (composite PK in Postgres). `UserId == -1` is a sentinel for "root/admin, implicit
  access to everything".
- `UserHousehold { Id: HouseholdAccess { ParentUserId, ChildUserId }, Permissions: List<HouseholdPermission> }`
  — indirect access: a child user inherits whatever the parent has, gated per-permission. Circular
  household relationships are explicitly rejected on creation.
- `APIKey { Id, UserId, Name, Key (hash), Permissions: List<HouseholdPermission> }`.

### Documents/attachments
`UploadedFiles { Name, Location, IsPending }` (`Models/Shared/UploadedFiles.cs`) is the **only**
file-metadata model, embedded (not a standalone table) as `List<UploadedFiles> Files` on every
record type that supports attachments. `Location` can be a local web-relative path (`/images/...`)
or, legitimately, an external URL — export tooling treats non-resolvable locations as link
attachments.

### ExtraFields (generic custom-fields mechanism)
- `ExtraField { Name, Value, IsRequired, FieldType: ExtraFieldType }` — embedded list on almost
  every record/vehicle.
- `RecordExtraField { Id (=ImportMode enum int value), ExtraFields: List<ExtraField> }` — the
  **template/definition** of which extra fields exist per record type, keyed by `ImportMode`,
  stored in the `extrafields` collection. Adding a new `ImportMode` value automatically plugs a new
  entity into this mechanism, plus CSV import and report export.

### Aggregation/report shapes (computed on the fly, not persisted)
`VehicleRecords`, `GenericReportModel` (flattened `{DataType: ImportMode, Date, Odometer,
Description, Notes, Cost, Files, ExtraFields, RequisitionHistory}` used to unify all record types
for CSV/report export), `VehicleInfo`, `KioskVehicleViewModel`, various `Models/Report/*` cost/MPG
shapes.

## Enums (`Enum/*.cs`)

| Enum | Values |
|---|---|
| `APIMethodType` | GET, POST, PUT, DELETE |
| `AutomatedEvent` | AllReminder, ReminderStateChanged, BackupEmail, UpdateRecurringTax, CleanTempFile, DeepClean |
| `DashboardMetric` | Default, TotalCost, CostPerMile |
| `ExtraFieldType` | Text, Number, Decimal, Date, Time, Location |
| `HouseholdPermission` | View, Edit, Delete |
| `ImportMode` | ServiceRecord, RepairRecord, GasRecord, TaxRecord, UpgradeRecord, ReminderRecord, NoteRecord, SupplyRecord, Dashboard, PlanRecord, OdometerRecord, VehicleRecord, InspectionRecord, EquipmentRecord — **doubles as the universal record-type discriminator** across PlanRecord.ImportMode, GenericReportModel.DataType, RecordExtraField.Id, InspectionRecordTemplateField.ActionItemType |
| `InspectionFieldType` | Text, Check, Radio |
| `KioskMode` | Vehicle, Plan, Reminder, Cycle |
| `PlanPriority` | Critical, Normal, Low |
| `PlanProgress` | Backlog, InProgress, Testing, Done |
| `ReminderIntervalUnit` | Months, Days |
| `ReminderMetric` | Date, Odometer, Both |
| `ReminderMileageInterval` | Other, then named steps 50…150000 miles |
| `ReminderMonthInterval` | Other, 1/3/6/12/24/36/60 months |
| `ReminderUrgency` | NotUrgent, Urgent, VeryUrgent, PastDue |
| `SkippedSetting` | SMTP, OIDC, Postgres, HTTPS |
| `TagFilter` | Exclude, IncludeOnly |

## Cost/price handling — key finding for Parts/Planned Work design

Cost today is a **single scalar `decimal Cost` field directly on each record** — no itemization,
no purchase/transaction entity: `GenericRecord.Cost`, `GasRecord.Cost`, `TaxRecord.Cost`,
`InspectionRecord.Cost`, `PlanRecord.Cost` (estimated), `SupplyRecord.Cost` (cost of the whole
purchased lot). `Vehicle.PurchasePrice`/`SoldPrice` are likewise bare scalars.

Total vehicle cost (`VehicleLogic.GetVehicleTotalCost`) sums `Cost` across Service + Collision +
Upgrade + Tax + Gas records (Supply/Note/Plan costs are excluded from this particular rollup).

**Implication for Phase 5 (Parts)**: `SupplyRecord` needs to split into a Part catalog entity
(reusable across vehicles/fitments) + a Purchase/Transaction entity (owns cost, quantity, date,
supplier) — keeping the existing `SupplyUsageHistory`-style ledger-line pattern for consumption
tracking, since that part of the design already works.

## Multi-vehicle / multi-user structure

Every domain record carries a plain `int VehicleId` FK; no navigation properties — joins happen in
application code via `Get*ByVehicleId` calls. Vehicle ownership is **not** a field on `Vehicle`,
it's the `UserAccess` join entity (many-to-many, `UserData` ↔ `Vehicle`). `UserLogic.FilterUserVehicles`
is the single enforcement point that turns "all vehicles" into "vehicles this user (or their
household parents) can see".

## File/attachment storage mechanics

Upload flow (`Controllers/FilesController.cs` + `Helper/FileHelper.cs`): files land in
`data/temp/{guid}{ext}` first (`UploadedFiles.IsPending = true`), then get promoted to a permanent
folder (`images/`, `documents/`, etc.) via `MoveFileFromTemp` once the parent record actually
saves. `FileHelper` also does path-traversal-guarded path resolution, whole-tree ZIP backup/restore,
temp-file cleanup, and orphaned-file garbage collection (walks every record type's `Files` list to
compute the "still referenced" set).

## Reuse opportunities for the Car Tracker target spec

- **Parts** (Phase 5): extend `SupplyRecord`/`SupplyUsage`/`SupplyUsageHistory`
  (`Models/Supply/*`, table `supplyrecords`) rather than building fresh — split part-catalog from
  purchase-lot as noted above.
- **Planned Engineering Work** (Phase 6): extend `PlanRecord` (`Models/PlanRecord/*`, table
  `planrecords`) — it already has priority/progress/Kanban state, reminder linkage, a target
  `ImportMode`, and parts requisitioning. `PlanRecordTemplate`'s reuse-the-input-as-storage pattern
  is a workable template mechanism to keep.
- **Attachments** (Phase 10): `UploadedFiles` + `IFileHelper` is already multi-entity-capable —
  new entities just add `List<UploadedFiles> Files` and reuse `FilesController`/`FileHelper`
  unchanged.
- **Extra Fields**: adding new `ImportMode` values for new entity types (e.g. a Part or Planned
  Work record type, if kept distinct from `PlanRecord`) plugs them into the existing custom-field,
  CSV import, and report-export machinery for free.

## Target-state notes (Phase 1 reconciliation, 2026-08-17)

Full requirement-level detail and evidence lives in `REQUIREMENTS.md`. Entity-level implications:

- **`SupplyRecord` splits into two entities at Phase 5**: a `Part` catalog entity (part number,
  manufacturer, description, category — reusable, no cost/quantity) and a `PartPurchase`/
  `PartTransaction` entity (owns cost, quantity, date, supplier, references a `Part`). The existing
  `SupplyUsage`/`SupplyUsageHistory` ledger-line pattern for consumption tracking is preserved
  as-is — it already does the right thing (immutable quantity+cost snapshot at time of use), it
  just currently points at a conflated entity.
- **`PlanRecord.Progress` (`PlanProgress` enum) needs new stages at Phase 6**: today
  `Backlog/InProgress/Testing/Done`; target needs explicit `Idea/Costed/PartsSourced/InProgress/Done`.
  Also needs an `ActualCost` field distinct from the existing `Cost` (estimated) field — actual
  cost should be populated at completion time, likely from the sum of consumed
  `SupplyUsageHistory`/`PartPurchase` costs once Phase 5 lands.
- **`OdometerRecord` needs a `Source` field at Phase 9** (`Manual`/`Service`/`MOT`) — currently
  absent even though multiple code paths (manual entry, `OdometerLogic.AutoInsertOdometerRecord`)
  already implicitly know their own source; this is a matter of capturing what's already known at
  each call site, not inferring anything new.
- **`UploadedFiles` needs a document-type field at Phase 10** (Invoice/MOT/V5C/Insurance/
  Photograph/Datasheet/Other) — currently just `{Name, Location, IsPending}`. Existing attachments
  without a type should default to "Other", not require a data migration/backfill.
- **New at Phase 8, no existing analog**: government-data adapter response models (DVLA vehicle
  record, DVSA MOT history entry) — these are read-only, externally-sourced shapes, not stored the
  same way as the mutable domain entities above; keep them out of the LiteDB/Postgres
  `I*DataAccess` pattern used for user-owned data, per `CLAUDE.md`'s "keep authoritative external
  data read-only in the domain model" principle.
