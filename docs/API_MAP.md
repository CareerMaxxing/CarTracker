# API_MAP.md

Baseline API/controller surface of LubeLogger as cloned, established during Phase 0 reconnaissance
(2026-08-17). Describes what **exists today**.

## Architecture note

`APIController` and `VehicleController` are declared `partial class` and physically split across
many files, each compiling into one large class:

- **`Controllers/APIController.cs` + `Controllers/API/*.cs`** (11 files) → `APIController`. Mounted
  with explicit `[Route("/api/...")]` attributes. This is the **pure JSON REST API** — documented
  at runtime via `/API` (loads `defaults/api.json`, rendered through a self-hosted API-explorer
  view, "swigarette.js" — effectively a lightweight Swagger UI). Designed for external
  integrations/automation (e.g. Home Assistant, scripts). Supports cookie, Basic, and API-key auth.
- **`Controllers/VehicleController.cs` + `Controllers/Vehicle/*.cs`** (11 files) → `VehicleController`.
  Conventional MVC routing (`{controller=Home}/{action=Index}/{id?}`). Actions mostly return
  `PartialView(...)` HTML fragments consumed by the server-rendered frontend's jQuery AJAX calls —
  not a documented public API, and not what the main UI's own JS talks to via JSON.

**Both tracks call into the same `Logic/`, `External/*DataAccess`, and `Helper/` layers** — no
duplicated business logic, only duplicated HTTP surface/response shaping. When adding new
endpoints for Car Tracker, the `/api/*` track is the one to extend for anything programmatic;
follow its existing pairing convention (`AddXJson`/`AddX`, `APIKeyFilter` + `CollaboratorFilter`).

## Auth model recap (full detail in ARCHITECTURE.md)

Single scheme `"AuthN"`, default policy requires it. Four credential paths into one identity:
(1) auth disabled → fake root-user identity; (2) `ACCESS_TOKEN` cookie; (3) HTTP Basic; (4) API key
(header `x-api-key` or `?apiKey=`), only accepted under `/api`, `/kiosk`, `/images`, `/documents`.
Unauthenticated → redirect to `/Login` (or 401 for `api` controller). Forbidden → redirect to
`/Error/Unauthorized` (or 403 for `api`/`APIAuth` role).

## Authorization summary

| Scope | Mechanism |
|---|---|
| Default | `RequireAuthenticatedUser()` global policy |
| Truly anonymous | `ThemeController` (whole class — login page needs CSS before auth), `LoginController` (pre-auth flows), `APIController.GetServerHealth` (`/health`) |
| Standard authenticated | `APIController`, `VehicleController`, `HomeController`, `FilesController`, `KioskController` (class-level `[Authorize]`) |
| Admin-only | `AdminController` (whole class, `IsAdmin` role) |
| Root-only | `MigrationController` (whole class); scattered actions in `APIController` (server setup/backup/cleanup/demo-restore/reminder-broadcast), `HomeController` (translation/theme/widget/OIDC-import/server-config/test-email/root-account/extra-fields, at `/setup`), `FilesController` (translation/theme upload, delete, backup, restore), `LoginController` (root credential bootstrap/destroy) |
| Per-vehicle access | `TypeFilter(CollaboratorFilter)` → `IUserLogic.UserCanEditVehicle`; single or multi (`vehicleIds`) mode; `vehicleId==0` special-cased for shop-wide supplies |
| Stricter vehicle-owner-only | `TypeFilter(StrictCollaboratorFilter)` → `UserCanDirectlyEditVehicle` (owner, not just collaborator) — used for delete/collaborator-management |
| API-key permission scoping | `TypeFilter(APIKeyFilter, Arguments=[HouseholdPermission])` on write endpoints — checks the key's granted permission set |

## `/api/*` — APIController endpoints

| Method | Route | Verb | Auth | Description |
|---|---|---|---|---|
| `Index` | `/API` | GET | Authorize | Renders API doc/explorer page |
| `WhoAmI` | `/api/whoami` | GET | Authorize | Caller's username/email/admin/root flags |
| `GetServerHealth` | `/health` | GET | **Anonymous** | DB health check + version |
| `GetServerInformation` | `/api/info` | GET | Authorize | Locale, currency symbol, date format, version |
| `ServerVersion` | `/api/version` | GET | Authorize | Current/latest version |
| `Vehicles` | `/api/vehicles` | GET | Authorize | Vehicles visible to caller |
| `VehicleInfo` | `/api/vehicle/info` | GET | Authorize | Stats for one/all vehicles |
| `AdjustedOdometer` | `/api/vehicle/adjustedodometer` | GET | Authorize + CollaboratorFilter | Applies odometer offset/multiplier |
| `AddVehicleJson`/`AddVehicle` | `/api/vehicles/add` | POST | Authorize | Create vehicle |
| `DeleteVehicle` | `/api/vehicles/delete` | DELETE | Authorize + APIKeyFilter(Delete) | Delete vehicle + all records |
| `UpdateVehicleJson`/`UpdateVehicle` | `/api/vehicles/update` | PUT | Authorize + APIKeyFilter(Edit) | Update vehicle |
| `UploadDocument` | `/api/documents/upload` | POST | Authorize | Upload to `data/documents/` |
| `SendReminders` | `/api/vehicle/reminders/send` | GET | Root only | Email reminder notifications |
| `GetExtraFields` | `/api/extrafields` | GET | Authorize | Export custom field definitions |
| `MakeBackup` | `/api/makebackup` | GET | Root only | ZIP backup (`output=download\|email`) |
| `GetTempFiles` | `/api/tempfiles` | GET | Root only | List temp folder |
| `CleanUp` | `/api/cleanup` | GET | Root only | Clear temp (+ unlinked files if `deepClean`) |
| `RestoreDemo` | `/api/demo/restore` | GET | Root only | Restore demo dataset |
| `GetGovernmentDataForVehicle` | `/api/vehicle/governmentdata` | GET | Authorize + CollaboratorFilter | Mocked DVLA tax/MOT status + DVSA MOT test history, looked up by `Vehicle.LicensePlate` (see `IDVLAAdapter`/`IDVSAAdapter`, Phase 8) |

Per-record-type CRUD (identical shape × 11 record types, all under `Controllers/API/*.cs`):

| Record type | List all | Per-vehicle | Add | Update | Delete |
|---|---|---|---|---|---|
| Gas | `/api/vehicle/gasrecords/all` | `/api/vehicle/gasrecords` (CollaboratorFilter) | `/gasrecords/add` | `/gasrecords/update` | `/gasrecords/delete` |
| Service | `/servicerecords/all` | `/servicerecords` | `/servicerecords/add` | `/servicerecords/update` | `/servicerecords/delete` |
| Repair (collision) | `/repairrecords/all` | `/repairrecords` | `/repairrecords/add` | `/repairrecords/update` | `/repairrecords/delete` |
| Upgrade | `/upgraderecords/all` | `/upgraderecords` | `/upgraderecords/add` | `/upgraderecords/update` | `/upgraderecords/delete` |
| Tax | `/taxrecords/all` (+ GET `/taxrecords/check`) | `/taxrecords` | `/taxrecords/add` | `/taxrecords/update` | `/taxrecords/delete` |
| Supply | `/supplyrecords/all` | `/supplyrecords` | `/supplyrecords/add` | `/supplyrecords/update` | `/supplyrecords/delete` |
| Plan | `/planrecords/all` | `/planrecords` | `/planrecords/add` | `/planrecords/update` | `/planrecords/delete` |
| Odometer | `/odometerrecords/all` (+ GET `/latest`, PUT `/recalculate`) | `/odometerrecords` | `/odometerrecords/add` (+`autoIncludeEquipment`) | `/odometerrecords/update` | `/odometerrecords/delete` |
| Note | `/notes/all` | `/notes` | `/notes/add` | `/notes/update` | `/notes/delete` |
| Reminder | `/reminders/all` (+ GET `/api/calendar`) | `/reminders` | `/reminders/add` | `/reminders/update` | `/reminders/delete` |
| Equipment | `/equipmentrecords/all` | `/equipmentrecords` | `/equipmentrecords/add` | `/equipmentrecords/update` | `/equipmentrecords/delete` |

Every `.../add` and `.../update` is a pair: `[Consumes("application/json")] ...Json([FromBody] X)`
delegating to a form-bound overload, so every write accepts both JSON and form/multipart. Delete
takes `?id=`.

## `/Vehicle/*` — VehicleController (conventional MVC)

Core actions (`Controllers/VehicleController.cs`): `Index(vehicleId)` (detail page),
`AddVehiclePartialView`/`GetEditVehiclePartialViewById` (modals), `SaveVehicle`, `DeleteVehicle(s)`,
collaborator management (`GetVehiclesCollaborators`, `AddCollaboratorsToVehicles`,
`RemoveCollaboratorsFromVehicles`), `SearchRecords`/`SearchRecordsByTags` (full-text/tag search
across all record types for a vehicle), `CheckRecordExist`, `GetMaxMileage`, `MoveRecord(s)`
(move between categories), `DeleteRecords` (bulk, any type), `AdjustRecordsOdometer`,
`DuplicateRecords`/`DuplicateRecordsToOtherVehicles`.

Per-resource partials (`Controllers/Vehicle/*.cs`), one file per record type, each following the
same CRUD-partial-view pattern (`Get*ByVehicleId`, `Save*ToVehicleId`, `GetAdd*PartialView`,
`Get*ForEditById`, `Delete*ById`): `GasController`, `ServiceController`, `RepairController`,
`UpgradeController`, `TaxController`, `SupplyController` (+ shop-wide supply endpoints gated by
`GetServerEnableShopSupplies`), `PlanController` (+ template conversion actions), `OdometerController`
(+ recalculation/duplication), `NoteController` (+ pinning), `ReminderController` (+ recurring
push-back), `InspectionController` (templates + dynamic checklist fields), `EquipmentController`,
`ImportController` (CSV import/export per record type — **the actual "import your data" feature**,
distinct from `MigrationController` below), `ReportController` (per-vehicle analytics: cost
tables/charts, MPG by month, reminder makeup, vehicle image map, history timeline, custom widgets).

## `/Admin/*` — AdminController (`IsAdmin` role required)

User/token management: list users+tokens, generate/delete registration tokens (batch via
comma-separated emails), delete user (cascades access/config/households/API keys), grant/revoke
admin, manage a user's household, reset/revoke passwords. Does **not** touch server-wide settings —
that's `HomeController.Setup`/`WriteServerConfiguration` (root-only, at `/setup`: locale, Postgres
connection, upload extensions, branding, MOTD, webhooks, custom widgets, SMTP, OIDC, registration
toggle, reminder thresholds, cookie lifespan, Kestrel, notification config).

## `/Login/*` — LoginController (mostly anonymous)

`Index`, `Registration`, `ForgotPassword`, `ResetPassword` (pages) · `GetRemoteLoginLink`/
`RemoteAuth`/`RemoteAuthDebug` (OIDC flow) · `Login`/`Register`/`RegisterOpenIdUser`/
`SendRegistrationToken`/`RequestResetPassword`/`PerformPasswordReset` (POST actions) ·
`CreateLoginCreds`/`DestroyLoginCreds` (root only, bootstrap/remove root credentials) ·
`LogOut` (`[Authorize]`).

## `/Files/*` — FilesController

`HandleFileUpload`/`HandleMultipleFileUpload` (upload to `data/temp/`) · `HandleTranslationFileUpload`/
`HandleThemeFileUpload` (root only, specialized moves) · `DeleteFiles` (root only) · `MakeBackup`/
`RestoreBackup` (root only) · `UploadCoordinates` (map overlay CSV) · `PreviewFile`.

## `/Migration/*` — MigrationController (root only)

**Not** a user-data import feature (that's `ImportController` above) — this is the **LiteDB ⇄
PostgreSQL storage-engine migration tool**, only active when `POSTGRES_CONNECTION` is configured.
`Export` pulls every Postgres table into a fresh LiteDB file and zips it; `Import` reads a LiteDB
backup and bulk-inserts every row into Postgres, auto-creating schema/tables. Covers all ~20
record/config tables.

## `/Kiosk/*` — KioskController (`[Authorize]`, despite the name)

`Index(exclusions, kioskMode)` (display page) · `KioskContent` (renders vehicle/plan/reminder
partial for non-excluded, non-sold vehicles) · `GetKioskVehicleInfo` (CollaboratorFilter). One of
the four path prefixes that accepts API-key auth.

## `/css/theme.css` — ThemeController (`[AllowAnonymous]`)

Serves the active CSS theme dynamically (user pref → server default) — public because the login
page needs styling before auth.

## `HomeController` (`/`, `/Home/*`)

Large grab-bag: `Index`, `Garage` (dashboard cards), `Calendar`, `Settings`/`WriteToSettings`
(per-user config), account/API-key/household self-service, root-only server administration
(translation editor, custom widgets, `Setup`), `Error()`.

## Conventions to follow when adding new endpoints

- No shared base controller — current user via
  `int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))`, roles via
  `User.IsInRole(nameof(UserData.IsRootUser))`/`IsAdmin`.
- Vehicle scoping: `IUserLogic.FilterUserVehicles` for lists, `CollaboratorFilter`/
  `StrictCollaboratorFilter` for single-record access — prefer the filter attributes over inline
  checks.
- Response shape: `Json(...)`, writes wrapped in `OperationResponse` (`Succeed`/`Failed`/
  `Conditional`, `{Success, Message}`).
- Mutating actions publish events: `_eventLogic.PublishEvent(userId, WebHookPayload.Generic(message,
  eventKey, username, entityId))` (e.g. `"vehicle.add"`, `"bulk.delete"`) — replicate this for new
  endpoints to keep the webhook/audit trail complete.
- Culture-invariant JSON: check `_config.GetInvariantApi()` / `culture-invariant` header for
  numeric/date-bearing API responses.
- No global exception-handling middleware for API controllers — try/catch + `_logger.LogError` +
  `Json(OperationResponse.Failed(...))` per action.
- File uploads: temp-then-move (`data/temp/` → permanent folder via `IFileHelper.MoveFileFromTemp`
  on confirm/save).
