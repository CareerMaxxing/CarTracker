# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 8 — Government Data
Current task:       PHASE-08-01 (see docs/execution/PHASE_08.md) — mocked DVLA/DVSA adapters +
                     Dashboard integration
Status:             Complete. Curl-verified: real vehicle's government data endpoint + rendered
                     Dashboard HTML both correct and internally consistent; empty-license-plate case
                     verified on a throwaway vehicle (deleted after). Not yet shown to the user live
                     in browser - offering that before moving on, per the user's 2026-08-17
                     instruction (mid-Phase-8) to resume verify-with-me-before-continuing between
                     every phase, superseding the earlier "finish all the phases" blanket approval.
Last completed:      Phase 7 finished (idempotent plan completion + full ImportMode coverage, see
                     PHASE_07.md). Phase 8: built IDVLAAdapter/IDVSAAdapter + MockDVLAAdapter/
                     MockDVSAAdapter (deterministic, seeded by registration number, internally
                     consistent tax/MOT status vs. dates, mutually consistent Make/Colour/FuelType
                     between the two mocks via a shared MockGovernmentDataGenerator helper), a new
                     GET /api/vehicle/governmentdata endpoint, and a new Dashboard/Report-tab panel
                     (first real use of Phase 2's .status-badge and .ct-empty-state primitives).
                     Looked up by Vehicle.LicensePlate specifically (not the configurable
                     VehicleIdentifier display field). Self-caught and fixed one bug before shipping:
                     first pass generated Tax/MOT status independently of their due/expiry dates,
                     producing nonsensical combos (e.g. "Taxed" next to a 2021 due date) - fixed by
                     deriving status from the dates instead of picking both independently.
Next task:           Show the user the new Dashboard panel and confirm it looks right, then ask
                     before starting Phase 9 (Mileage/Odometer) or anything else - per the user's
                     renewed phase-by-phase check-in instruction and CLAUDE.md's phase-boundary rule.
Known blockers:      1. No browser/screenshot tool in this environment - continue the "review locally
                        as I go" workflow; this phase's panel is fully server-rendered/read-only so
                        HTML-diff verification covers it well, but a live look is still worth offering.
                     2. Global search technical approach undecided (REQUIREMENTS.md FR-SEARCH-01,
                        not urgent, resolve at Phase 11).
                     3. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice.
                     4. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items - all deferred to a finishing-touches pass, not
                        forgotten. Phase 8 added two: the MOT-status Garage badge (blocked-on-Phase-8
                        item from PHASE_03.md, now unblocked but not built) and the real-adapter swap
                        (blocked on credentials, a mandatory stop condition per CLAUDE.md).
Open decisions:      None blocking. User has re-confirmed (2026-08-17, mid-Phase-8) they want to
                     verify and approve each phase before the next one starts, same as the original
                     workflow prior to the brief "finish all the phases" instruction.
Do not:              Start Phase 9 or beyond without the user's go-ahead - this is now explicit again
                     per the user's 2026-08-17 message, not just CLAUDE.md's default rule. Do not
                     re-litigate or re-surface items already tracked in DEFERRED.md as if they were
                     forgotten - they're intentionally parked. Do not assume SQLite is available
                     anywhere in this codebase. Do not assume a fresh vehicle/user has any tabs
                     visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only; check/set
                     it first or use the API directly when testing a vehicle detail page. When
                     touching interactive markup, keep using the diff-verify-then-user-confirms
                     workflow, calibrating review depth to actual interactive risk (drag-and-drop
                     needs live testing; a server-rendered read-only panel is fully covered by HTML
                     diffing). When calling record-add API endpoints for testing, field names/casing
                     are inconsistent across export models and dates must match the server's locale
                     (dd/mm/yyyy here) - check the relevant *ExportModel class in Models/Shared/
                     ImportModel.cs first rather than guessing. Part is NOT vehicle-scoped (global
                     catalog) but PartPurchase IS (VehicleId, 0=shop-wide). PartPurchase.
                     QuantityRemaining must be set explicitly by the caller, never by ToPartPurchase().
                     PlanRecord.ActualCost is preferred over Cost (estimate) by the completion-
                     conversion logic when non-zero, for all 5 target record types. Government data
                     is looked up by Vehicle.LicensePlate, never VehicleIdentifier (display-only
                     field, can point at a custom ExtraField like VIN) - LicensePlate can legitimately
                     be blank, handle as Found=false, not an error. IDVLAAdapter/IDVSAAdapter take
                     only a registration number (matches the real APIs) - do not widen the interface
                     to accept a Vehicle object even though the mock could theoretically use it.
Last validation:     dotnet build (0 errors, 224 warnings, unchanged - no new warnings introduced);
                     GET /api/vehicle/governmentdata?vehicleId=1 (real vehicle, read-only) verified
                     internally consistent after the status/date fix; 400 on missing vehicleId, 404
                     on nonexistent vehicleId; throwaway vehicle with identifier=VIN and no
                     LicensePlate (id 2, created and deleted via API) verified Found=false end-to-end
                     including the rendered .ct-empty-state panel; full Dashboard/Report partial HTML
                     fetched and diffed for correct panel markup/badge classes/MOT list rendering —
                     2026-08-17. Not yet shown to the user live in browser.
Last commit:         8c700a6 — "Phase 8: mocked DVLA/DVSA government data adapters + Dashboard
                     panel" — user confirmed and approved moving to Phase 9, 2026-08-17.
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
