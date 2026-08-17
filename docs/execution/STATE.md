# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 14 — V1 Hardening (Increment 2: Automated Test Project)
Current task:       PHASE-14-02 (see docs/execution/PHASE_14.md) — stand up the test project deferred
                     since Phase 7
Status:             Complete. Tests/CarCareTracker.Tests.csproj (xUnit + WebApplicationFactory) now
                     exists, 10 tests passing covering Phase 7's idempotency fix, both Phase 12
                     Part/PartPurchase reliability bugs, Phase 14 Increment 1's upload blocklist, and
                     Phase 9's odometer regression warning. Verified: suite passes twice in a row
                     (stability), real dotnet build/dotnet run unaffected, real vehicle/data
                     directory confirmed untouched by any test run (not just inferred from the
                     isolation design - checked directly).
Last completed:      Phase 14 Increment 1 (security review) finished and pushed. User then chose
                     "automated test project" as Increment 2 (of four offered: tests/error-handling/
                     accessibility/pause-here). Implementation: added `public partial class Program`
                     to Program.cs; scaffolded the new test project and added it to the .sln; hit and
                     fixed a build-glob conflict (the main csproj was compiling Tests/'s xUnit-only
                     source files into itself, since Tests/ nests under the main project's root -
                     fixed by excluding Tests/**/*.cs from the main project); built
                     CarTrackerWebApplicationFactory implementing the CWD-isolation approach Phase 7
                     had already identified, then hit two further problems NOT anticipated in the
                     original investigation, both diagnosed empirically (temporary diagnostic output,
                     not guessed at): (1) WebApplicationFactory's content-root auto-detection assumes
                     a "solution/ProjectName/ProjectName.csproj" layout and guessed wrong for this
                     repo's flat layout; (2) `dotnet test`'s VSTest host sets the process's working
                     directory to the test assembly's own bin output folder before test code runs, so
                     Directory.GetCurrentDirectory() couldn't be trusted for finding the real app
                     either. Fixed by walking up from AppContext.BaseDirectory looking for
                     CarCareTracker.csproj by name - the one approach depending on neither assumption.
                     Wrote 4 test classes (10 tests), debugged one real test-authoring mistake (used
                     an invalid PlanPriority value), got to a stable, real, all-passing suite.
Next task:           Phase 14 takes increments - accessibility, mobile/responsive validation, and
                     performance review remain open. Ask the user for the next priority, or whether
                     to pause here.
Known blockers:      1. No browser/screenshot tool in this environment - this increment was backend/
                        tooling-only (a new test project), no UI surface to review live.
                     2. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. The "Test infrastructure" entry there is now
                        marked done (not deferred) with a pointer to what it covers and what it
                        doesn't (everything outside those 4 flows - not exhaustive coverage, a
                        starting point).
Open decisions:      What to prioritize next within Phase 14 (or elsewhere) - ask the user rather
                     than assume. Standing instruction: verify and approve each increment/phase
                     before the next one starts.
Do not:              Assume Phase 14 is "done" - security review and automated tests are the only
                     two increments complete; accessibility/mobile/performance remain open. Do not
                     assume `dotnet test` can be run from just anywhere - CarTrackerWebApplicationFactory
                     finds the real app's content root by walking up from AppContext.BaseDirectory
                     looking for CarCareTracker.csproj, which works regardless of invocation
                     directory, but the test project must stay physically under the main repo for
                     that walk-up to succeed. Do not add new files directly under Tests/ expecting
                     them to run in parallel with each other - every test class must carry
                     [Collection("CarTracker")] to share the one serialized fixture instance
                     (Directory.SetCurrentDirectory inside the fixture is process-wide; parallel
                     instances would race). Do not be alarmed by an empty-ish leftover folder in the
                     OS temp directory named CarTrackerTests_* after a test run - LiteDBHelper isn't
                     IDisposable so cleanup is best-effort; it never contains real data, just
                     throwaway test-run state, and is a known accepted trade-off, not a bug to chase.
                     If the main CarCareTracker.csproj is ever modified, remember it explicitly
                     excludes Tests/**/*.cs/Content/None from its own globs - don't remove that
                     exclusion without re-verifying the main app still builds standalone. Do not
                     re-add the old OnPrepareResponse-based auth checks on the static file routes -
                     they're structurally incapable of blocking content; any future change to those
                     routes' auth must go through the gate middleware in Program.cs. Do not implement
                     CSRF tokens or a CSP header without discussing first - both are real scope and
                     neither was requested yet. Do not assume SQLite is available anywhere in this
                     codebase. Do not assume a fresh vehicle/user has any tabs visible beyond
                     Dashboard - VisibleTabs defaults to [Dashboard] only. Do not add a "MOT"/"Part"/
                     etc. (any non-record-type) value to the ImportMode enum. When any controller
                     does a "move files from temp"/reconstruct-UploadedFiles step, it MUST explicitly
                     copy every field it wants to keep. When adding a new entity type with its own
                     Files/attachments, wire it into GetVehicleDocuments/DeleteVehicleRecords/
                     ClearUnlinkedDocuments in Logic/VehicleLogic.cs. Any enum embedded directly in a
                     type used as a JSON request-body wire format needs its own
                     JsonStringEnumConverter. When calling record-add API endpoints for testing
                     (curl or the new test project), field names/casing are inconsistent across
                     export models and dates must match the server's locale (dd/mm/yyyy here). Note
                     records require both Description AND NoteText. PlanRecord's Priority must be one
                     of Critical/Normal/Low (not e.g. "Medium" - a real mistake caught while writing
                     this increment's tests). Some fields (e.g. Vehicle.HasOdometerAdjustment) are
                     MVC-only, not exposed on the API's *ImportModel DTOs. Part is NOT vehicle-scoped
                     (global catalog) but PartPurchase IS (VehicleId, 0=shop-wide).
                     PartPurchase.QuantityRemaining must be set explicitly by the caller, never by
                     ToPartPurchase(). PlanRecord.ActualCost is preferred over Cost (estimate) by the
                     completion-conversion logic when non-zero. Government data is looked up by
                     Vehicle.LicensePlate, never VehicleIdentifier. OdometerRecord.Source must be
                     preserved (not reset to Manual) on manual edits of auto-inserted records. The
                     root/dev user's config (EnableAuth=false) reads directly from
                     data/config/userConfig.json but is cached in-memory for up to 1 hour - restart
                     the app after editing it.
Last validation:     dotnet build (main solution, 0 errors); dotnet test Tests/CarCareTracker.Tests.csproj
                     (10/10 passing, run twice for stability); dotnet run (real app confirmed starting
                     normally, real vehicle - id 1, BMW Z4 - confirmed present and unaffected) —
                     2026-08-17.
Last commit:         a5fbbe6 — "Record Phase 14 security review commit hash in STATE.md" (this
                     increment's commit not yet made - pending user confirmation first).
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
- Automated tests: `dotnet test Tests/CarCareTracker.Tests.csproj` from the repo root (or anywhere -
  content root is found by walking up for CarCareTracker.csproj, not by invocation directory). Fully
  isolated from real data; safe to run anytime.
