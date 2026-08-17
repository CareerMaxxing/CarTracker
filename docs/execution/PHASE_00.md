# PHASE_00 — Baseline Reconnaissance

## Task packet

```
TASK ID: PHASE-00
TITLE: Baseline Reconnaissance
OBJECTIVE: Establish the clean LubeLogger baseline before any product changes — confirm it builds
  and runs, and produce architecture/data-model/API/UI documentation sufficient to plan Car
  Tracker's target requirements against it.
INPUTS: Clean clone of hargata/lubelog at d:/Personal/CarTracker/lubelog (main, unmodified),
  the original Car Tracker engineering specification (locked decisions, phase list).
ALLOWED SCOPE: Read-only inspection of the existing codebase; build/run verification; writing
  documentation files; repository/git connection setup (private repo, remotes); installing missing
  local tooling needed to verify the baseline (.NET SDK).
NON-SCOPE: Any product feature work, UI redesign, or domain-model changes. No product functionality
  was touched.
IMPLEMENTATION REQUIREMENTS:
  - Confirm the application starts and core functionality works.
  - Map repository structure, frontend/backend/DB/storage/API/auth/config/build/test systems.
  - Map existing database entities and relationships.
  - Map existing API endpoints/services.
  - Inventory all major UI screens and workflows.
  - Identify extension points and a first retain/modify/extend/replace/remove read.
  - Document build/run/test procedures.
DELIVERABLES: docs/SYSTEM_BASELINE.md, docs/ARCHITECTURE.md, docs/DATA_MODEL.md, docs/API_MAP.md,
  docs/UI_INVENTORY.md, docs/execution/ROADMAP.md, docs/execution/STATE.md, CLAUDE.md.
ACCEPTANCE CRITERIA:
  - [x] Repo connected to the private GitHub repo (origin), upstream preserved for future syncing.
  - [x] Application builds with 0 errors.
  - [x] Application starts and serves requests (/, /health, /css/theme.css all verified 200).
  - [x] All five Phase 0 documentation deliverables exist and are populated with concrete findings
        (not placeholders).
  - [x] No product functionality changed.
  - [x] Results committed to git.
VALIDATION COMMANDS:
  dotnet build
  dotnet run --urls http://localhost:5299   (then curl :5299/, :5299/health, :5299/css/theme.css)
STOP CONDITION: Reconnaissance complete, documentation exists, no product functionality changed,
  results committed → stop and report for human review (per ROADMAP.md checkpoint "After Phase 0").
```

## What was done

1. Connected the local clone to the private GitHub repo `CareerMaxxing/CarTracker` as `origin`
   (renamed the original `hargata/lubelog` remote to `upstream` so future upstream fixes can still
   be pulled). Force-pushed local `main` over GitHub's auto-generated placeholder initial commit
   (a single-line README with no real content — confirmed before overwriting).
2. Found no .NET SDK installed in the environment (only a stale `dotnet.exe` muxer stub, no SDK
   toolset). Installed .NET 10 SDK (10.0.400) via `winget install Microsoft.DotNet.SDK.10`.
3. Ran four parallel read-only reconnaissance passes over the codebase (backend architecture &
   bootstrap, data model, API surface, UI/frontend) and synthesized them into the doc set below.
4. Verified the baseline directly: `dotnet build` (0 errors), `dotnet run` (starts cleanly,
   `/health` reports a passing DB connection check), confirmed `data/` (runtime DB + uploads) stays
   gitignored so running the app doesn't dirty the working tree.
5. Wrote `CLAUDE.md` (operating rules), `docs/execution/ROADMAP.md` (full phase list from the
   spec), `docs/SYSTEM_BASELINE.md`, `docs/ARCHITECTURE.md`, `docs/DATA_MODEL.md`,
   `docs/API_MAP.md`, `docs/UI_INVENTORY.md`, and this file.

## Key findings that affect future phases

- **Persistence is LiteDB/Postgres, not SQLite** — the original spec's Phase 11 assumption ("prefer
  the existing SQLite FTS5 direction") does not hold. Global search needs a fresh technical
  decision. See `ARCHITECTURE.md` and `SYSTEM_BASELINE.md`.
- **No test project exists anywhere in the repo.** Flagged in `CLAUDE.md` as something to stand up
  the first time a task needs it, rather than a Phase 0 blocker.
- **Strong existing analogs for two "new" domain concepts**: `SupplyRecord` for Parts (Phase 5) and
  `PlanRecord` for Planned Engineering Work (Phase 6) — both should be extended, not rebuilt. See
  `DATA_MODEL.md` "Reuse opportunities".

## Result

Phase 0 complete per the acceptance criteria above. Per `ROADMAP.md`, the next step is the human
review checkpoint ("After Phase 0: review actual LubeLogger architecture") before Phase 1
(Requirements & Architecture Reconciliation) begins.
