# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 0 — Baseline Reconnaissance
Current task:       PHASE-00 (see docs/execution/PHASE_00.md)
Status:             Complete — awaiting human review checkpoint
Last completed:     Phase 0 reconnaissance: repo connected to private GitHub (CareerMaxxing/CarTracker),
                     .NET 10 SDK installed, build/run verified, all Phase 0 docs written
                     (SYSTEM_BASELINE.md, ARCHITECTURE.md, DATA_MODEL.md, API_MAP.md, UI_INVENTORY.md),
                     CLAUDE.md and docs/execution/ROADMAP.md created.
Next task:           Human review of Phase 0 findings, then Phase 1 (Requirements & Architecture
                     Reconciliation) per docs/execution/ROADMAP.md — do not start Phase 1 without
                     that review.
Known blockers:      None blocking further work, but two open items carried forward (see
                     docs/SYSTEM_BASELINE.md "Known blockers / open items"):
                     1. Global search technical approach undecided (no SQLite FTS5 available —
                        actual backends are LiteDB/Postgres).
                     2. No test project exists yet in the repo.
Open decisions:      None pending from Phase 0 itself. Human review of the baseline docs is the
                     gating step before Phase 1 can begin.
Do not:              Start Phase 1 (or any product feature work) before the human review checkpoint
                     for Phase 0 has happened. Do not assume SQLite is available anywhere in this
                     codebase (see ARCHITECTURE.md correction).
Last validation:     dotnet build (0 errors, 209 pre-existing nullable warnings) + dotnet run
                     smoke test (/, /health, /css/theme.css all 200) — 2026-08-17.
Last commit:         (pending — Phase 0 documentation commit, created immediately after this file)
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
