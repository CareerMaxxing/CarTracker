# ARCHITECTURE.md

Baseline architecture of LubeLogger as cloned, established during Phase 0 reconnaissance
(2026-08-17). This describes what **exists today** — Phase 1 will layer target-state notes on top
where Car Tracker needs to diverge or extend.

## Overview

Single ASP.NET Core MVC web app (`CarCareTracker.csproj`, `net10.0`, `Microsoft.NET.Sdk.Web`),
server-rendered Razor views + jQuery frontend (no SPA framework, no `fetch()` usage anywhere — all
AJAX is jQuery calling MVC actions that return partial-view HTML). There is a **separate, parallel
JSON REST API** (`/api/*`) used for external integrations, documented via a self-hosted API
explorer at `/API`. The core web UI does not consume this API — it's a fully independent surface
over the same domain logic. See `API_MAP.md`.

No test project exists anywhere in the repo (`CarCareTracker.sln` has exactly one project; no
xUnit/NUnit/Moq packages).

## Application bootstrap

`Program.cs` is a single top-level minimal-hosting file (~230 lines) that does all wiring:

- **Config layering**: `appsettings.json` → environment variables → `data/config/userConfig.json`
  → `data/config/serverConfig.json` (both optional, reload-on-change). If `LUBELOGGER_SECRETS_PATH`
  is set, a key-per-file provider is added (Docker/K8s secrets pattern).
- **Startup side effects**: creates `data/`, `data/images`, `data/documents`, `data/translations`,
  `data/themes`, `data/temp`, `data/config` if missing; migrates legacy pre-`data/` paths forward.
- **DI registration** is a large flat list in `Program.cs`, no modules/extension-method grouping:
  - `ILiteDBHelper` is **always** registered as a singleton, regardless of DB backend choice.
  - Branches on whether `POSTGRES_CONNECTION` is set: if so, registers ~19 `PG*DataAccess`
    singletons + `PGDBHealthCheck`; otherwise registers the LiteDB-backed equivalents +
    `DBHealthCheck`. This is a full swap of the data layer, not a hybrid (see "Data storage" below).
  - Helper singletons: `IFileHelper`, `IGasHelper`, `IEquipmentHelper`, `IReminderHelper`,
    `IReportHelper`, `IConfigHelper`, `ITranslationHelper`, `IMailHelper`.
  - Logic singletons: `ILoginLogic`, `IUserLogic`, `IOdometerLogic`, `IVehicleLogic`,
    `INotificationLogic`.
  - `AutomatedEventLogic` registered as an `IHostedService` only if `LUBELOGGER_AUTO_EVENTS` is
    truthy (scheduled reminder refresh, recurring tax rollover, temp-file cleanup).
  - SignalR (`IEventLogic`/`EventLogic`, hub at `/api/ws`) for live UI updates.
  - Auth: custom scheme `"AuthN"` (see Authentication below) set as the **default** policy —
    every controller/action requires it unless explicitly opted out.
  - Kestrel/form limits raised to `int.MaxValue` for large attachment uploads.
- **Middleware pipeline order**: exception handler → `UseStaticFiles()` (default `wwwroot`) → three
  more `UseStaticFiles()` calls mapping `data/images` → `/images`, `data/documents` → `/documents`,
  `data/temp` → `/temp`, each with an `OnPrepareResponse` hook that sets `no-store` caching and
  redirects unauthenticated requests to `/Login` (manual auth-gating, since static files are served
  before `UseAuthorization()`) → unauthenticated `data/translations` → `/translations` →
  `BufferBody` middleware for `/api/*` JSON requests (enables re-reading the body later, used by
  `QueryParamFilter`) → `UseRouting()` → `UseAuthorization()` → default MVC route
  `{controller=Home}/{action=Index}/{id?}` → SignalR hub `/api/ws`.

## Configuration & settings persistence

Two on-disk JSON files under `data/config/` (paths in `Helper/StaticHelper.cs`):

- **`userConfig.json`** — the *root user's* config, including `EnableAuth`, `UserNameHash`,
  `UserPasswordHash`. Written by `ConfigHelper.SaveUserConfig` and `LoginLogic.CreateRootUserCredentials`.
- **`serverConfig.json`** — server-wide settings (SMTP, OIDC, webhook URL, MOTD, Kestrel endpoints,
  locale overrides, cookie lifespan, branding, skipped-settings flags). Modeled by
  `Models/Settings/ServerConfig.cs`, whose properties carry `[JsonPropertyName]` mappings to their
  equivalent env-var keys (e.g. `POSTGRES_CONNECTION`, `LUBELOGGER_MOTD`, `LUBELOGGER_DOMAIN`).

Non-root users' individual settings live in the DB (`IUserConfigDataAccess`), cached per-user in
`IMemoryCache` for 1 hour. Config resolution is mostly dynamic string lookups via `IConfiguration`
indexers rather than strongly-typed `IOptions<T>` binding (a few sections — Kestrel, MailConfig,
OpenIDConfig, ReminderUrgencyConfig, NotificationConfig, SkippedSettings — are bound with
`GetSection(...).Get<T>()`).

`appsettings.json` doubles as both ASP.NET config and the default `UserConfig` values (dark mode,
`EnableAuth: false` by default, tab order/visibility, unit preferences, root credentials blank).

## Data storage

**Important correction to the original planning assumption**: the persistence layer is **not
SQLite**. It is:

- **LiteDB** (embedded, file-based NoSQL/document DB) — default backend, single file at
  `data/cartracker.db`, wrapped by `Helper/LiteDBHelper.cs`. Always registered in DI even when
  Postgres is active (harmless — nothing else references it in that mode).
- **PostgreSQL** (via Npgsql) — opt-in via a non-empty `POSTGRES_CONNECTION` config value. When
  set, *every* data-access singleton flips to its `PG*DataAccess` implementation. Postgres is used
  purely as a **JSONB document store**: each table is
  `app.<table> (id serial pk, vehicleid int, data jsonb)`, with the whole POCO serialized via
  `System.Text.Json` into the `data` column — not normalized columns, no EF Core, no migrations
  framework. DDL is hand-rolled `CREATE TABLE IF NOT EXISTS` per data-access class constructor.

This matters directly for Phase 11 (Global Search): there is **no SQLite FTS5** to build on. Any
full-text search approach needs to work for both a LiteDB collection and a Postgres JSONB table, or
needs to explicitly decide to support only one backend for search.

A dedicated **`MigrationController`** (root-only, `/Migration/*`) exists purely to move the *entire*
dataset between LiteDB and Postgres (export zips a LiteDB file from Postgres tables; import bulk-
inserts a LiteDB backup into Postgres). This is not a CSV/user-data import feature — see
`API_MAP.md` for the separate CSV import/export (`ImportController`).

**File storage** (not DB-backed): `Helper/FileHelper.cs` manages `data/images`, `data/documents`,
`data/translations`, `data/themes`, `data/temp`, plus `data/widgets.html`. Attachments are plain
files on disk referenced by path strings stored in DB records (see `DATA_MODEL.md` §
`UploadedFiles`), not blobs. `FileHelper` also provides whole-tree backup/restore as a ZIP
(`MakeBackup`/`RestoreBackup`), path-traversal guards, and orphaned-file garbage collection.

## Authentication & authorization

- Single custom scheme `"AuthN"` (`Middleware/Authen.cs`) is the entire auth engine — not ASP.NET
  Identity. It is registered as the default policy, so `[Authorize]` is implicitly global.
- **If `EnableAuth == false`** (the shipped default): every request is treated as an authenticated
  synthetic root/admin user. The whole app runs "unauthenticated" in this mode.
- **If `EnableAuth == true`**, `Authen` tries, in order: (1) `ACCESS_TOKEN` cookie, DataProtection-
  encrypted, containing `{UserData, ExpiresOn}`; (2) HTTP Basic Auth header; (3) API key via
  `x-api-key` header or `?apiKey=` query param, restricted to paths under `/api`, `/kiosk`,
  `/images`, `/documents`.
- Login (`LoginController`/`LoginLogic`) compares SHA-256 password hashes — no salt, no bcrypt.
  Root credentials come from `userConfig.json`; regular users are DB-backed.
- **OIDC** is a hand-rolled Authorization Code flow (`LoginController.RemoteAuth`) with optional
  PKCE and JWKS-based JWT validation — not a library like `Microsoft.AspNetCore.Authentication.OpenIdConnect`.
- **Roles**: only two coarse claims exist — `IsRootUser`, `IsAdmin` (gates `AdminController` and
  scattered root-only actions). Fine-grained access is a separate **vehicle collaborator/household**
  model, enforced by MVC action filters rather than `[Authorize]` policies:
  - `Filter/CollaboratorFilter.cs` — household-aware per-vehicle check (`View`/`Edit`/`Delete`).
  - `Filter/StrictCollaboratorFilter.cs` — direct-owner-only variant (used for destructive ops).
  - `Filter/APIKeyFilter.cs` — checks an API key's granted permission set.
  - `Filter/QueryParamFilter.cs` — re-reads the buffered JSON body to backfill filter arguments
    (e.g. `vehicleId`) for API endpoints where it isn't in the route.

This access-control model (per-vehicle collaborators + household inheritance + API key scoping,
`Enum/HouseholdPermission.cs` = View/Edit/Delete) is the multi-tenancy story for the whole app —
any new domain entity (Parts, Planned Work) needs to plug into it, not invent a parallel one.

## Layered architecture

Pattern is **Controllers → (Logic and/or Helper) → External/*DataAccess**, but not strictly
layered — controllers frequently inject `*DataAccess` interfaces directly and bypass `Logic/`:

- **`Controllers/`** — `VehicleController` and `APIController` are declared `partial class` and
  physically split across many files (`Controllers/Vehicle/*.cs`, `Controllers/API/*.cs`) — each
  compiles into one large class. No shared base controller class; common behavior (get current
  user id, role checks) is duplicated per controller.
- **`Logic/`** — cross-entity business rules: `LoginLogic`, `UserLogic` (collaborators/households/
  API keys), `OdometerLogic`, `VehicleLogic` (aggregation, recurring-tax engine, cascading delete),
  `Logic/Event/*` (`EventLogic`/`EventHubLogic` for SignalR, `NotificationLogic` for webhooks,
  `AutomatedEventLogic` as a hosted job). Depends only on `External.Interfaces` and `Helper/`.
- **`Helper/`** — cross-cutting utilities: `ConfigHelper`, `FileHelper`, `GasHelper`,
  `EquipmentHelper`, `ReminderHelper`, `ReportHelper`, `MailHelper` (MailKit/SMTP),
  `TranslationHelper`, `LiteDBHelper`, `StaticHelper` (large stateless-constants/utility namespace).
- **`External/`** — despite the name, this is the **data-access layer**, not third-party
  integrations: `External/Interfaces/I*DataAccess.cs` (one contract per entity) +
  `External/Implementations/{Litedb,Postgres}/*DataAccess.cs` (the two swappable backends). This is
  the one place with clean interface segregation.
- **`Models/`** — organized by feature folder, mixing persistence POCOs and MVC view models in the
  same folders. See `DATA_MODEL.md`.
- **`MapProfile/`** — a single file (`ImportMappers.cs`); despite the AutoMapper-evoking name, it's
  a **CsvHelper `ClassMap`** for CSV import column aliasing. There is no object-mapping library in
  use — DTO↔entity conversion is hand-written `.ToXRecord()` methods per input class.
- **`Filter/`** — the vehicle-scoped authorization filters described above.
- **`Views/`** mirrors `Controllers/` — classic server-rendered Razor + jQuery/Bootstrap, no SPA
  framework. See `UI_INVENTORY.md`.

## External integrations

Actual third-party/outbound integration code lives in `Helper/`/`Controllers/`, not `External/`:

- **Email**: `Helper/MailHelper.cs` via MailKit — registration/reset tokens, reminder digests,
  test-SMTP email, automated DB backup emails.
- **OIDC/OAuth2**: hand-rolled against `IHttpClientFactory` + `Microsoft.IdentityModel.JsonWebTokens`.
- **Webhooks**: `NotificationLogic` posts a `WebHookPayload` to a configurable `LUBELOGGER_WEBHOOK`
  URL on vehicle/record events (a lightweight audit trail — new endpoints should follow this
  pattern for consistency).
- **Translations**: fetched from `https://hargata.github.io/lubelog_translations` at runtime.
- **Update/sponsor checks**: GitHub Releases API and a GitHub Pages–hosted sponsors JSON.
- **CSV import/export**: CsvHelper library (not a network integration, but a notable dependency).

## Build & deployment

- `CarCareTracker.csproj`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`. Packages:
  `CsvHelper`, `LiteDB`, `MailKit`, `Microsoft.IdentityModel.JsonWebTokens`, `Npgsql`. No test
  packages.
- `Dockerfile`: multi-stage, `mcr.microsoft.com/dotnet/sdk:10.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`,
  cross-arch build support, exposes port 8080.
- `docker-compose.yml` (baseline, LiteDB), `docker-compose.postgresql.yml` (adds a `postgres:18`
  sidecar + `POSTGRES_CONNECTION`), `docker-compose.traefik.yml` (adds reverse-proxy labels). All
  mount `data:/App/data` and `keys:/root/.aspnet/DataProtection-Keys` (cookie-encryption keys must
  persist across restarts or existing sessions/encrypted config break).

## Local build/run/test procedures (verified 2026-08-17)

```bash
# Prerequisite: .NET 10 SDK (not present by default on a fresh machine — verify with `dotnet --list-sdks`)
dotnet build                                   # 0 errors, ~209 pre-existing nullable warnings
dotnet run --urls http://localhost:5299        # starts, auto-creates data/ on first run
curl http://localhost:5299/health              # {"status":"pass", "checks":[{"name":"...DB...","status":"pass"}]}
```

No automated test suite exists yet to run — see `CLAUDE.md` "Test infrastructure note".

## Notable extension points for Car Tracker

- **Government data adapters** (Phase 8): none exist today. Will be new `External/Interfaces` +
  `External/Implementations` following the exact same interface-segregation pattern already used
  for storage backends (LiteDB vs Postgres) — that pattern is proven and idiomatic here.
- **Parts / Planned Work** (Phases 5–7): strong existing bases to extend, not build fresh — see
  `DATA_MODEL.md` (`SupplyRecord`, `PlanRecord`).
- **Documents** (Phase 10): `UploadedFiles` + `IFileHelper` already work as a shared, multi-entity
  attachment mechanism — reuse directly.
- **Search** (Phase 11): no FTS5; needs a fresh decision informed by the LiteDB/Postgres split
  above.

## Target-state notes (Phase 1 reconciliation, 2026-08-17)

Full requirement-level detail and evidence lives in `REQUIREMENTS.md`; this section records the
architectural implications specifically.

- **Government data adapters** (FR-GOV-01): confirmed as the pattern to follow —
  `External/Interfaces/IDVLAAdapter.cs` + `IDVSAAdapter.cs`, `External/Implementations/Mock/
  MockDVLAAdapter.cs` + `MockDVSAAdapter.cs`, registered as singletons in `Program.cs` the same way
  the LiteDB/Postgres branch already works. No changes to the existing DI branching logic are
  needed — this is strictly additive.
- **Planned Work → Service Record is an existing code path needing hardening, not new
  architecture** (FR-PLAN-04/05): `Controllers/Vehicle/PlanController.cs:277-378`
  (`UpdatePlanRecordProgress`) already performs the conversion. Phase 7's job is (a) add an
  idempotency guard — check `existingRecord.Progress` before re-running the conversion block, or
  equivalent — and (b) add the missing `GasRecord`/`TaxRecord` branches. No new controller action,
  route, or data-access interface is needed for the core mechanism.
- **Global search** (FR-SEARCH-01): no architectural decision made yet. `SearchRecords`/
  `SearchRecordsByTags` in `VehicleController` are the existing single-vehicle implementation to
  extend or wrap for cross-vehicle search at Phase 11 — see `REQUIREMENTS.md` for the candidate
  approaches to evaluate at that time.
- **Parts split** (FR-PART-01/02/03): architecturally, this means a new `Part` entity + a new
  `PartPurchase`/`PartTransaction` entity replacing the dual role `SupplyRecord` currently plays,
  following the same `I*DataAccess` + LiteDB/Postgres implementation pattern as every other entity.
  Exact field-level shape is deferred to Phase 5 (implementation), not decided here.
- **No changes needed** to the auth model, collaborator/household permission model, file storage
  mechanics, or build/deployment setup to support any Phase 1-reconciled requirement — all new
  entities plug into the existing patterns as-is.
