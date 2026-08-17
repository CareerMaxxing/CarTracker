# CLAUDE.md — Operating Rules

Non-negotiable operating rules for any coding agent (Claude Code or otherwise) working in this
repository. This file is authoritative. If anything here conflicts with a request made in
conversation, this file wins unless the human owner explicitly overrides it in writing (and that
override should then be reflected back into this file or `docs/execution/STATE.md`).

## What this project is

This repository was originally a clean clone of [LubeLogger](https://github.com/hargata/lubelog)
(`upstream` remote). It is being evolved into **Car Tracker**: a personal, local-first vehicle
engineering and maintenance management system, built *on top of* LubeLogger rather than as a
rewrite. Full context: `docs/SYSTEM_BASELINE.md` and the original engineering specification this
project follows (see `docs/execution/ROADMAP.md` for the phase list derived from it).

## Locked project decisions (do not revisit without explicit human sign-off)

- **Repository**: private GitHub repo `CareerMaxxing/CarTracker` (this repo's `origin`).
- **Runtime**: local PC / localhost only. No cloud deployment.
- **Existing data**: none — treat as a fresh install, do not design migrations around
  pre-existing user data.
- **Mobile**: responsive web only for now. Native mobile is deferred.
- **Offline/sync**: local-first, no cloud sync / PocketBase-style architecture unless a later
  requirement explicitly demands it.
- **AI/OCR**: deferred. Do not implement in V1.
- **Government data (DVLA/DVSA)**: mocked adapters only until explicitly told to integrate real
  credentials.
- **Foundation**: build on LubeLogger's existing domain model and codebase; do not rebuild generic
  vehicle-management functionality from scratch.
- **UI**: a major UI/UX overhaul is in scope, but existing functionality must be preserved through
  the transition (parity before replacement).

## Operating model

```
Specification → Phase → Task → Implement → Test → Inspect → Accept → Commit → Update State → Continue/Stop
```

Work is organized into numbered phases (`docs/execution/ROADMAP.md`), each broken into tasks. Each
task is a contract (see Task Packet Format below) — implement exactly what it says, not more, not
less.

## MUST

- Read this file before starting work.
- Read `docs/execution/STATE.md` before starting work — it is the persistent execution state.
  Do not assume state from conversation history alone.
- Read the current phase's document (`docs/execution/PHASE_NN.md`) before starting work on it.
- Inspect the existing implementation before modifying it.
- Work only within the current task's defined scope.
- Implement the smallest complete solution that satisfies the task.
- Create or update automated tests where applicable (see "Test infrastructure" note below — as of
  Phase 0 there is no test project yet; standing one up is in-scope the first time a task needs it).
- Run the relevant validation commands (build at minimum; tests once they exist).
- Inspect the resulting diff for unrelated changes before committing.
- Verify every acceptance criterion for the task explicitly.
- Update documentation when implementation changes anything documented in `docs/`.
- Update `docs/execution/STATE.md` after every completed task.
- Create a git commit for each completed task (small, coherent commits).
- Continue to the next task automatically only when the current phase's scope permits it.

## MUST NOT

- Start a future phase early.
- Invent product requirements. If a material decision is ambiguous, stop and ask.
- Perform speculative feature development.
- Rewrite working systems merely because another architecture looks cleaner.
- Introduce major dependencies without justification.
- Delete existing functionality before replacement parity exists and is tested.
- Perform destructive data migrations without explicit approval.
- Add cloud infrastructure, sync services, or native mobile infrastructure without an explicit
  requirement.
- Implement AI/OCR in V1.
- Use real DVLA/DVSA credentials during the mocked-adapter phase.
- Continue working indefinitely after the current phase's defined scope is complete.
- Declare a task or phase complete without evidence (build passing, tests passing, acceptance
  criteria checked one by one).

## Mandatory stop conditions

Stop and ask the human owner if any of the following occurs:

- A destructive database/data migration is required.
- Existing user-visible functionality must be deleted.
- A new external service or a significant new dependency is required.
- A fundamental architectural decision is not specified anywhere in `docs/`.
- Requirements conflict with each other.
- Credentials or secrets are required for anything.
- A security-sensitive architectural decision is needed.
- Acceptance criteria can't be satisfied without changing a higher-level decision already locked
  in this file.
- Tests reveal an unrelated pre-existing regression whose fix would expand scope materially.
- The task turns out to be materially larger than what was specified.

## Definition of done

A task is done only when **all** of the following are true, with evidence, not assertion:

1. Implementation matches the task's scope (not more).
2. Relevant tests exist and pass.
3. Every acceptance criterion has been checked off explicitly.
4. Documentation in `docs/` is updated if the implementation changed anything it describes.
5. The diff has been inspected for unrelated changes.
6. `docs/execution/STATE.md` has been updated.
7. A git commit has been created for the task.

## Task packet format

Every execution task should be defined with:

```
TASK ID:
TITLE:
OBJECTIVE:
INPUTS:
ALLOWED SCOPE:
NON-SCOPE:
IMPLEMENTATION REQUIREMENTS:
DELIVERABLES:
ACCEPTANCE CRITERIA:
VALIDATION COMMANDS:
STOP CONDITION:
```

Treat it as a contract. Ordinary implementation choices inside the contract are fine; silently
changing the contract itself is not.

## Repository control documents

```
CLAUDE.md                          (this file)
docs/
├── SYSTEM_SPEC.md                 (Phase 1+) what the system is intended to be
├── REQUIREMENTS.md                (Phase 1+) functional/non-functional requirements + acceptance criteria
├── ARCHITECTURE.md                actual + target technical architecture
├── DATA_MODEL.md                  domain entities, fields, relationships, ownership
├── API_MAP.md                     existing + target API surface
├── UI_SPEC.md                     (Phase 2+) target UX/design system and interaction rules
├── UI_INVENTORY.md                current screens and their migration status
├── SYSTEM_BASELINE.md             Phase 0 baseline snapshot (build/run/test procedures, findings)
└── execution/
    ├── ROADMAP.md                 ordered phases and tasks
    ├── STATE.md                   persistent execution state — read this first, every session
    ├── PHASE_00.md, PHASE_01.md, ...
```

## Git strategy

- Small, coherent commits. Avoid giant rewrite commits.
- `origin` = `CareerMaxxing/CarTracker` (this project). `upstream` = `hargata/lubelog` (the real
  LubeLogger project — fetch from it if a task needs to check what upstream has done, but do not
  push to it).
- Never commit secrets, credentials, local database files (`data/` is gitignored — keep it that
  way), or personal data.
- A completed task should normally produce one reviewable commit.

## Test infrastructure note (as of Phase 0)

The upstream LubeLogger codebase, as cloned, has **no test project** (confirmed: `CarCareTracker.sln`
contains exactly one project, no xUnit/NUnit/Moq packages anywhere). "Create or update automated
tests where applicable" in the MUST list above therefore also covers standing up a test project
the first time a task needs one — do this as part of that task, scoped to what the task needs, not
as a speculative up-front framework build-out.

## Environment note (as of Phase 0)

- .NET SDK: 10.0.400, installed via winget (`Microsoft.DotNet.SDK.10`). Target framework of
  `CarCareTracker.csproj` is `net10.0`.
- Default local run: `dotnet run --urls http://localhost:5299` from the repo root. Data/DB/uploaded
  files persist under `data/` (gitignored, auto-created on first run).
- No `.NET SDK` was present in this environment prior to Phase 0; if working in a fresh environment
  and `dotnet --list-sdks` is empty, install it before attempting to build.
