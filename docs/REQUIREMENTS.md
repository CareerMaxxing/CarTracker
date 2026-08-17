# REQUIREMENTS.md

Functional and non-functional requirements for Car Tracker, reconciled against the verified
LubeLogger baseline (Phase 0). Each requirement carries a traceability classification —
**EXISTING** (works today, no change needed), **MODIFY** (existing capability needs a behavior
change), **EXTEND** (existing capability/entity grows new fields or actions, old behavior kept),
**REPLACE** (existing capability removed and rebuilt), **NEW** (nothing like it exists today) —
plus the evidence it's based on. Acceptance criteria here are requirement-level; each phase's
`PHASE_NN.md` will break these into task-level acceptance criteria when that phase starts.

No REPLACE-classified requirements were identified during Phase 1 — nothing found in Phase 0
conflicts with the target spec badly enough to warrant discarding working functionality, which is
consistent with `CLAUDE.md`'s "do not rewrite working systems merely because another architecture
looks cleaner."

## Traceability matrix (summary)

| Capability | Classification | Existing basis | Target phase |
|---|---|---|---|
| Vehicle CRUD, ownership/collaborators | EXISTING | `Vehicle`, `UserAccess`, `UserHousehold` | Phase 4 |
| Service/Repair/Upgrade/Tax/Gas history | EXISTING | `GenericRecord` subclasses, `GasRecord`, `TaxRecord` | Phase 4 |
| Reminders/upcoming work | EXISTING | `ReminderRecord` + `ReminderHelper` urgency calc | Phase 3 |
| Odometer history | EXTEND | `OdometerRecord` (add `Source`, regression flag) | Phase 9 |
| Documents/attachments | EXTEND | `UploadedFiles` + `IFileHelper` (add document type) | Phase 10 |
| Parts | EXTEND | `SupplyRecord`/`SupplyUsage`/`SupplyUsageHistory` (split part vs. purchase) | Phase 5 |
| Planned engineering work (board) | EXTEND | `PlanRecord` (extend `PlanProgress` stages) | Phase 6 |
| Planned work → service record | MODIFY | `PlanController.UpdatePlanRecordProgress` (add idempotency, complete coverage) | Phase 7 |
| Government data (DVLA/DVSA) | NEW | none | Phase 8 |
| Garage/dashboard | EXTEND | `Home/Garage`, `_GarageDisplay` | Phase 3 |
| Global search | EXTEND | `SearchRecords`/`SearchRecordsByTags` (per-vehicle only today) | Phase 11 |
| UI design system | EXTEND | Bootstrap 5.3.2 + `site.css` + working theme system | Phase 2 |
| Backup/restore/reliability | EXISTING | `FileHelper.MakeBackup`/`RestoreBackup` | Phase 12 |
| AI/OCR | — (deferred) | n/a | Phase 13 (do not build in V1) |
| Automated test coverage | NEW | none (no test project exists) | Phase 14 (stood up incrementally as needed before then) |

## FR — Vehicles (Phase 4)

**FR-VEH-01** (EXISTING). Users can create, read, update, and delete vehicles with year/make/model/
license-plate/identifier, purchase/sold price and date, electric/diesel/hours flags, image, tags,
and custom extra fields. *Evidence*: `Models/Vehicle/Vehicle.cs`, `VehicleController.SaveVehicle`/
`DeleteVehicle(s)`. *Acceptance*: no change required; verify parity through any UI rework in
Phase 2/4.

**FR-VEH-02** (EXISTING). Vehicle access can be shared with other users directly (collaborator) or
transitively (household), each with View/Edit/Delete granularity. *Evidence*: `UserAccess`,
`UserHousehold`, `CollaboratorFilter`/`StrictCollaboratorFilter`. *Acceptance*: preserve unchanged;
any new domain entity (Parts, Planned Work) must respect this model rather than invent a parallel
permission system.

## FR — Maintenance / Service History (Phase 4)

**FR-MAINT-01** (EXISTING). Service, repair (collision), upgrade, tax, and gas records are fully
CRUD-able per vehicle, each with cost, notes, tags, attachments, and extra fields. *Evidence*:
`GenericRecord` subclasses + `GasRecord`/`TaxRecord`, full CRUD in both `/api/*` and
`/Vehicle/*` controller tracks. *Acceptance*: no change required for V1.

## FR — Parts (Phase 5)

**FR-PART-01** (EXTEND). A Part must be a reusable catalog entity independent of any single
purchase — part number, manufacturer, description, category, fitment — usable across multiple
vehicles/purchases. *Evidence for gap*: today `SupplyRecord` conflates "the part" with "a specific
purchase lot of that part" — cost and quantity live directly on `SupplyRecord`, and there's no
concept of the same part being referenced by more than one purchase record.
`Models/Supply/SupplyRecord.cs`. *Acceptance*: a Part can exist with zero or many associated
purchases; editing a Part's catalog data does not require touching purchase history.

**FR-PART-02** (EXTEND). Price/cost belongs to a purchase/transaction, not to the part itself; a
part's price history is the set of its purchases over time. *Evidence*: `SupplyUsageHistory
{Id, Date, PartNumber, Description, Quantity, Cost}` already models an immutable ledger line
distinct from a record's own `Cost` — the closest existing building block, but it's embedded per
consuming record rather than a first-class transaction entity with its own identity tied to a
Part. *Acceptance*: creating two purchases of the same part at different prices does not alter
either purchase's historical price; a part's displayed "price" is derived (e.g. most recent or
average purchase), never stored redundantly on the part.

**FR-PART-03** (EXTEND). A part can be associated with a fitment (vehicle/engine) without being
exclusively owned by one vehicle. *Evidence*: `SupplyRecord.VehicleId == 0` already models a
shared "shop-wide" supply concept, gated by `GetServerEnableShopSupplies` — a workable existing
seed for this, needs generalizing from a boolean shop/not-shop split into real fitment
associations. *Acceptance*: a part usable on multiple vehicle fitments can be consumed against
records for any of them without duplication.

**FR-PART-04** (EXTEND). Parts consumption against a maintenance record must keep the existing
requisition/ledger and inventory-adjustment behavior. *Evidence*: `SupplyUsage`/
`SupplyUsageHistory`, `VehicleLogic.RestoreSupplyRecordsByUsage` (returns consumed parts to
inventory on delete/undo). *Acceptance*: no regression in existing supply-consumption/restoration
behavior once Parts are split from purchases.

## FR — Planned Engineering Work (Phase 6)

**FR-PLAN-01** (EXTEND). Planned work must track vehicle, objective, priority, estimated cost,
actual cost, associated parts, documents, notes, and completion information through a Kanban-style
pipeline: Idea → Costed → Parts Sourced → In Progress → Done. *Evidence*: `PlanRecord` already
has `Priority: PlanPriority`, `Cost` (estimated), `RequisitionHistory` (associated parts), `Files`,
`Notes`, and a `Progress: PlanProgress` pipeline — but the existing pipeline stages are
`Backlog/InProgress/Testing/Done`, which don't map cleanly onto the target's `Idea/Costed/Parts
Sourced/In Progress/Done`. There's also no "actual cost" distinct from `Cost` (estimated) today.
*Acceptance*: the pipeline exposes explicit Costed and Parts Sourced stages; actual cost is
recorded separately from estimated cost once work completes.

**FR-PLAN-02** (EXTEND). Planned work must support reusable templates. *Evidence*: LubeLogger
already has `PlanRecordTemplate` (stored as `PlanRecordInput`, table `planrecordtemplates`),
including supply-availability checking before creating a template. *Acceptance*: preserve this
mechanism; extend only if the new pipeline stages require new template fields.

## FR — Planned Work → Service Record (Phase 7)

**FR-PLAN-03** (EXISTING, verified working). Marking planned work as Done for a plan whose target
type is Service/Repair/Upgrade already creates the corresponding permanent record, carrying over
cost, notes, files, and parts requisition history, and pushes back any linked recurring reminders.
*Evidence*: `Controllers/Vehicle/PlanController.cs:277-378` (`UpdatePlanRecordProgress`).
*Acceptance*: no regression to this path for the three record types it already covers.

**FR-PLAN-04** (MODIFY — critical, mirrors the original spec's "must be idempotent" requirement
directly). The planned-work-to-service-record conversion **must be idempotent**: resubmitting a
completion for the same plan record must not create a duplicate service record. *Evidence for
gap*: `UpdatePlanRecordProgress` (`PlanController.cs:277-378`) unconditionally re-runs the
conversion block whenever called with `PlanProgress.Done`, regardless of the plan's prior
`Progress` value — there is no check for "already converted." A double-click, retry, or replayed
request on this endpoint today creates a second duplicate `ServiceRecord`/`CollisionRecord`/
`UpgradeRecord`. *Acceptance*: calling the completion action twice (or concurrently) on the same
plan record results in exactly one resulting service/repair/upgrade record, verified by a
dedicated test (see `CLAUDE.md` — this is exactly the kind of task that should stand up the first
real test coverage).

**FR-PLAN-05** (MODIFY). Completing planned work whose target type is Gas or Tax must also produce
the corresponding record. *Evidence for gap*: the same conversion block only branches on
`ImportMode.ServiceRecord`/`RepairRecord`/`UpgradeRecord` — `GasRecord` and `TaxRecord` (both
valid `ImportMode` values a `PlanRecord` can target) fall through with no branch, so completing
such a plan silently produces no record at all. *Acceptance*: completing a plan targeting any of
the five supported `ImportMode` values produces the matching record type; completing a plan
targeting an unsupported type either produces one or fails loudly — it must never silently no-op.

**FR-PLAN-06** (EXTEND). Preserve purchase prices at the time of completion, not current/live
prices. *Evidence*: `RequisitionHistory` is copied by value from the plan into the new record
today, which already achieves this for the three covered types — extend the same copy-by-value
approach to FR-PLAN-05's new branches and to whatever the Phase 5 Part/Purchase split produces.
*Acceptance*: editing a part's current price after a plan has been completed does not change the
historical cost recorded on the resulting service record.

## FR — Government Data (Phase 8)

**FR-GOV-01** (NEW). The system must be able to display DVLA-sourced vehicle data (tax/MOT status,
registration details) and DVSA-sourced MOT history, via adapters, with mocked implementations for
V1. *Evidence*: nothing exists today. *Design guidance*: follow the exact interface-segregation
pattern already proven in this codebase for swappable backends — `External/Interfaces/IDVLAAdapter`
+ `IDVSAAdapter`, with `MockDVLAAdapter`/`MockDVSAAdapter` implementations registered in DI the same
way `Program.cs` already branches between LiteDB and Postgres data-access implementations.
*Acceptance*: the domain model depends only on the adapter interfaces; swapping mock for real
adapters later requires no domain/controller changes. No real DVLA/DVSA credentials are used or
requested in V1 (mandatory stop condition in `CLAUDE.md` if this comes up).

## FR — Mileage / Odometer (Phase 9)

**FR-ODO-01** (EXTEND). Odometer readings must record their source (manual entry, service record,
MOT). *Evidence for gap*: `OdometerRecord` has no `Source` field today —
`Models/OdometerRecord/OdometerRecord.cs` has `InitialMileage`/`Mileage`/`Date`/`Notes`/etc. but no
provenance field, even though `OdometerLogic.AutoInsertOdometerRecord` already auto-inserts
readings from other actions (implying an implicit source that isn't captured explicitly).
*Acceptance*: every odometer reading has a `Source` value; existing auto-insert call sites are
updated to set it rather than leaving it blank/default.

**FR-ODO-02** (EXTEND). The system should flag suspicious mileage regressions rather than silently
accepting impossible data (e.g. a new reading lower than a prior one without an odometer-adjustment
flag set). *Evidence for gap*: `Vehicle.HasOdometerAdjustment`/`OdometerMultiplier`/
`OdometerDifference` exist for handling *intentional* odometer replacement/rollback, but no
validation was found that flags an *unintentional* regression at entry time.
*Acceptance*: entering a mileage value lower than the vehicle's last reported mileage (and
`HasOdometerAdjustment` is not set) produces a visible warning rather than being silently accepted.

## FR — Documents (Phase 10)

**FR-DOC-01** (EXTEND). Documents must be categorizable by type: Invoice, MOT, V5C, Insurance,
Photograph, Datasheet, Other. *Evidence for gap*: `UploadedFiles {Name, Location, IsPending}`
carries no type/category field today. *Acceptance*: uploaded attachments can be tagged with one of
the target document types; existing attachments without a type default to "Other" and remain
functional (no forced re-categorization, no data loss).

**FR-DOC-02** (EXISTING). Documents attach to vehicles, service records, and (once built) parts and
planned work, using the existing temp-then-move upload flow and backup/restore coverage.
*Evidence*: `FilesController`, `IFileHelper`, already multi-entity via embedded `List<UploadedFiles>
Files`. *Acceptance*: new entities (Parts, Planned Work extensions) reuse this mechanism unchanged.

## FR — Global Search (Phase 11)

**FR-SEARCH-01** (EXTEND). Provide a global, cross-vehicle, cross-entity search covering parts,
service records, planned work, documents, and vehicles. *Evidence for gap*: `SearchRecords`/
`SearchRecordsByTags` (`VehicleController`) already implement text/tag search across all record
types, but scoped to a single vehicle at a time — there is no cross-vehicle aggregate search today.
*Open technical decision (not resolved in Phase 1 — resolve at Phase 11 with current data volume
and both supported backends in mind)*: the original spec assumed SQLite FTS5, which doesn't exist
in this codebase (see `ARCHITECTURE.md`). Candidate approaches to evaluate at Phase 11: (a) extend
the existing in-application `SearchRecords` filtering logic to run across all of a user's visible
vehicles (simplest, no new infrastructure, may not scale well); (b) LiteDB's built-in indexing on
key fields (no FTS-equivalent, so this would be "search," not true full-text ranking); (c) a
Postgres `tsvector`/GIN-index approach, only available when running on the Postgres backend. This
is an ordinary implementation choice, not a locked product requirement — do not treat it as a stop
condition, but do record the decision in `ARCHITECTURE.md` when Phase 11 makes it.

## NFR — Reliability / Local-first (Phase 12)

**NFR-REL-01** (EXISTING). Full-system backup and restore as a ZIP archive covering DB, images,
documents, translations, themes, and config. *Evidence*: `FileHelper.MakeBackup`/`RestoreBackup`.
*Acceptance*: preserve as-is; extend backup/restore coverage to any new Part/Purchase/Government
adapter cache data introduced in later phases so backups stay complete.

**NFR-REL-02** (EXISTING). The database connection is health-checked at `/health` and reported at
startup. *Evidence*: `DBHealthCheck`/`PGDBHealthCheck`, verified live during Phase 0. *Acceptance*:
no change required.

## NFR — Non-goals (explicit, do not implement in V1)

**NFR-NONGOAL-01**: AI/OCR of any kind (Phase 13, explicitly deferred).
**NFR-NONGOAL-02**: Real DVLA/DVSA credentials or live government API calls — mocked adapters only.
**NFR-NONGOAL-03**: Cloud sync, PocketBase, or any multi-device sync architecture.
**NFR-NONGOAL-04**: Native mobile application — responsive web only.
**NFR-NONGOAL-05**: Data migration tooling for pre-existing user data — there is none to migrate
(fresh-install assumption, per `CLAUDE.md`).
