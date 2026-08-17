# SYSTEM_BASELINE.md

Phase 0 baseline summary. Full detail lives in the companion docs (`ARCHITECTURE.md`,
`DATA_MODEL.md`, `API_MAP.md`, `UI_INVENTORY.md`) — this is the top-level summary plus the
verified build/run/test procedure and a first-pass retain/modify/extend/replace/remove read on
existing functionality (Phase 1 will turn this into the full traceable matrix against target
requirements).

## What was verified

- Repo: `d:/Personal/CarTracker/lubelog`, git remotes `origin` = `CareerMaxxing/CarTracker`
  (private, this project) and `upstream` = `hargata/lubelog` (real LubeLogger, for pulling future
  upstream fixes). Clean clone, `main` branch, working tree clean at time of reconnaissance.
- **Stack**: ASP.NET Core MVC, C#, `net10.0`, server-rendered Razor + jQuery/Bootstrap frontend, no
  SPA framework, no test project.
- **Build**: `dotnet build` — 0 errors, 209 pre-existing nullable-reference warnings (not
  introduced by this project; not in scope to fix during Phase 0).
- **Run**: `dotnet run --urls http://localhost:5299` starts cleanly, auto-creates `data/` on first
  run (gitignored). `GET /` → 200, `GET /health` → 200 with a passing DB health check, `GET
  /css/theme.css` → 200. App version reported at startup: LubeLogger 1.7.0.
- **Environment gap found and fixed**: no .NET SDK was present on this machine before Phase 0
  (only a stale `dotnet.exe` muxer stub). Installed .NET 10 SDK (10.0.400) via winget
  (`Microsoft.DotNet.SDK.10`) to unblock the build/run verification above.

## System snapshot

| Aspect | Finding |
|---|---|
| Data storage | **LiteDB** (embedded, default, `data/cartracker.db`) or **PostgreSQL** (opt-in via `POSTGRES_CONNECTION`, used purely as a JSONB document store — not SQLite, correcting an assumption in the original planning spec) |
| Auth | Custom `"AuthN"` scheme (`Middleware/Authen.cs`), not ASP.NET Identity. `EnableAuth=false` by default (whole app runs as an implicit root user). Supports cookie, Basic, API key, and a hand-rolled OIDC flow when enabled. |
| Authorization | Two coarse role claims (`IsRootUser`, `IsAdmin`) + a separate per-vehicle collaborator/household permission model enforced via MVC action filters |
| API surface | Two parallel tracks over the same domain logic: `/api/*` (JSON REST, documented, external-integration-facing) and `/Vehicle/*`+`/Home/*`+etc. (conventional MVC, partial-view-returning, what the shipped UI actually calls) |
| Domain entities | Vehicle + 13 record types keyed by a shared `ImportMode` enum (Service/Repair/Gas/Tax/Upgrade/Reminder/Note/Supply/Plan/Odometer/Inspection/Equipment + Vehicle itself), all `VehicleId`-scoped, all with embedded `Files`/`Tags`/`ExtraFields` |
| File storage | Filesystem-based (`data/images`, `data/documents`, etc.), path strings stored in DB records, temp-then-move upload pattern, whole-tree ZIP backup/restore |
| Test infrastructure | None exists — `CarCareTracker.sln` has exactly one project |
| UI | 17+ distinct screen areas, ~200 `.cshtml` files, jQuery AJAX + server-rendered partials throughout, Bootstrap 5.3.2, a working (if ad hoc) dark-mode/custom-theme system already in place |

## First-pass retain / modify / extend / replace / remove read

This is a preliminary read to close out Phase 0 with something actionable; **Phase 1 is where this
becomes the formal traceable matrix** against the locked target requirements.

- **Retain as-is (for now)**: auth engine, collaborator/household permission model, file/attachment
  storage mechanics, LiteDB/Postgres data-access pattern, event/webhook publishing, CSV
  import/export, OIDC flow, backup/restore. These all work and nothing in the target spec demands
  changing them yet.
- **Extend, don't replace**: `SupplyRecord` → Parts domain (Phase 5); `PlanRecord` → Planned
  Engineering Work (Phase 6); `UploadedFiles`/`IFileHelper` → Documents (Phase 10); `ImportMode`/
  `ExtraField` mechanism → any new record type Car Tracker introduces.
  `ReminderRecord`↔`PlanRecord`↔`ServiceRecord` linkage already models a version of "planned work
  becomes a service record" — Phase 7's idempotent completion workflow should build on this
  existing linkage rather than inventing a parallel one.
- **New, no existing analog**: Government data adapters (Phase 8) — nothing like this exists today.
  Global search (Phase 11) — `SearchRecords`/`SearchRecordsByTags` exist per-vehicle but there's no
  cross-entity/cross-vehicle command-palette search, and no FTS5 to build on (see Architecture
  note above).
- **Modify carefully**: navigation/tab UI (triplicated markup, manual responsive breakpoints) is a
  strong Phase 2 target — but the underlying `ImportMode`/`TabOrder`/`VisibleTabs` data model
  driving it should probably be kept, just re-rendered.
- **Nothing identified for outright removal** during Phase 0 — no functionality inspected so far
  conflicts with the locked target decisions in `CLAUDE.md`.

## Known blockers / open items carried into Phase 1

- Global search approach needs a real decision now that SQLite/FTS5 isn't available (LiteDB has no
  built-in full-text index comparable to FTS5; Postgres could use `tsvector` but that's
  backend-specific and the app supports both backends).
- No test project exists; the first task that needs automated tests should stand one up scoped to
  what it needs (see `CLAUDE.md`).
