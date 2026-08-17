# PHASE_08 — Government Data

## Scope

Per `REQUIREMENTS.md` FR-GOV-01 and `CLAUDE.md`'s locked "Government data" decision: mocked
DVLA/DVSA adapters behind a domain-facing interface, no real credentials, no network calls.
Satisfies the core V1 acceptance scenario's "IDENTIFY VEHICLE → LOAD/VIEW GOVERNMENT DATA (mocked)"
step.

## Task packet

```
TASK ID: PHASE-08-01
TITLE: Mocked DVLA/DVSA adapters + Dashboard integration
OBJECTIVE: Give every vehicle a read-only, clearly-labelled mock government-data lookup (tax
  status, MOT status/expiry, MOT test history) reachable from both the API and the Vehicle
  Dashboard tab, architected so a real adapter is a drop-in swap later.
INPUTS: docs/ARCHITECTURE.md's Phase 1 target-state note (interface-segregation pattern to follow,
  same shape as the LiteDB/Postgres split), Models/Vehicle/Vehicle.cs (LicensePlate field),
  Controllers/Vehicle/ReportController.cs (existing Dashboard/Report tab composition).
ALLOWED SCOPE: New read-only DTOs mirroring the real DVLA Vehicle Enquiry Service / DVSA MOT
  History API response shapes; IDVLAAdapter/IDVSAAdapter interfaces; deterministic Mock
  implementations (no network, no credentials); DI registration; one new API endpoint; one new
  Dashboard-tab panel.
NON-SCOPE: Real HTTP-backed adapters (explicitly forbidden this phase); persisting government data
  via the I*DataAccess/LiteDB/Postgres pattern (it's externally-sourced and read-only, not owned
  state); an MOT-status Garage dashboard badge extending Phase 3's DashboardMetric system (flagged
  as a candidate in PHASE_03.md but not required for this phase's acceptance criteria - left for a
  later increment since it touches a different, already-shipped feature).
IMPLEMENTATION REQUIREMENTS:
  - DVLAVehicleData/DVSAMotHistory/DVSAMotTest/DVSAMotComment DTOs field-for-field mirroring the
    real APIs, plus Found (lookup success) and IsMockData (true for the mock, false once/if a real
    adapter ever exists) so the UI never misrepresents mock data as authoritative.
  - IDVLAAdapter.GetVehicleData(string registrationNumber) / IDVSAAdapter.GetMotHistory(string
    registrationNumber) - registration-number-only input, matching what the real APIs actually take
    (they don't accept a pre-known Make/Model/Year), so a real adapter is a true drop-in swap.
  - MockDVLAAdapter/MockDVSAAdapter: deterministic (seeded by the registration number, not
    wall-clock random) so repeated lookups for the same plate are stable; internally-consistent
    (Tax/MOT status derived from the generated due/expiry dates relative to today, not picked
    independently - avoids showing e.g. "Taxed" next to a 2021 due date); mutually consistent with
    each other (same seed source via a shared MockGovernmentDataGenerator helper, so DVLA's Make and
    DVSA's Make agree for the same plate) without either adapter depending on the other.
  - Registered as singletons in Program.cs, outside the LiteDB/Postgres branch (not backend-specific).
  - Looked up by Vehicle.LicensePlate specifically, not the configurable VehicleIdentifier display
    field (VehicleIdentifier can point at a custom ExtraField like VIN; a real DVLA/DVSA lookup is
    always keyed by the actual registration plate regardless of what the UI displays as the
    "identifier"). LicensePlate can legitimately be blank (only required when
    VehicleIdentifier=="LicensePlate") - both adapters and the UI handle that as a "Found=false"
    empty state, not an error.
  - API endpoint: GET /api/vehicle/governmentdata?vehicleId= (CollaboratorFilter, read access),
    returns a combined { dvlaData, motHistory } object.
  - Dashboard integration: new panel in the existing Report/Dashboard tab (Views/Vehicle/
    Report/_Report.cshtml), rendered synchronously as part of GetReportPartialView (matching how
    every other panel on that tab already works - it's not an AJAX-lazy-loaded tab like
    Documents/Parts). Uses the Phase 2 .status-badge primitive (first real adoption - green/neutral/
    red for Taxed/SORN/Untaxed and Valid/Not valid) and .ct-empty-state for the no-plate case.
DELIVERABLES: IDVLAAdapter/IDVSAAdapter + Mock implementations, API endpoint, Dashboard panel.
ACCEPTANCE CRITERIA:
  - GET /api/vehicle/governmentdata?vehicleId=<real vehicle> returns internally-consistent mock tax/
    MOT data plus MOT test history, all four fields (dvlaData.taxStatus/motStatus and their
    corresponding dates) mutually consistent.
  - Same registration number always returns the same data (determinism).
  - A vehicle with no LicensePlate (VehicleIdentifier set to something else) returns Found=false on
    both DVLA and MOT data, and the Dashboard shows the empty-state panel instead of blank/broken
    fields.
  - Dashboard tab HTML renders the new panel with correct status-badge classes for the returned
    statuses.
  - No changes to any other Dashboard panel's existing behavior.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against the real vehicle (read-only, no mutation) and a throwaway
  vehicle created with a non-LicensePlate identifier (deleted after): GET the API endpoint directly,
  GET the rendered Dashboard partial HTML, confirm both.
STOP CONDITION: Acceptance criteria met, verified via curl, changes committed. Live browser
  verification not required (no drag-and-drop/visual-only interaction - server-rendered read-only
  panel, fully verifiable via HTML diffing) but offered to the user as always.
```

## What was done

1. Confirmed the lookup key: `Vehicle.LicensePlate` (raw string field) rather than
   `Vehicle.VehicleIdentifier` (which only controls what's *displayed* as the vehicle's identifier
   in the UI, and can point at a custom `ExtraField` like VIN instead). A real DVLA/DVSA lookup is
   always keyed by the actual registration plate, so this is the correct field regardless of a
   user's `VehicleIdentifier` display preference.
2. Created the DTOs (`Models/GovernmentData/DVLAVehicleData.cs`, `DVSAMotHistory.cs` +
   `DVSAMotTest`/`DVSAMotComment`), deliberately mirroring the real APIs' field names/shapes so a
   future real adapter needs no domain or controller changes - just a new class implementing the
   same interface.
3. Created `External/Interfaces/IDVLAAdapter.cs`/`IDVSAAdapter.cs` - single-method interfaces taking
   only a registration number, matching what the real APIs actually accept.
4. Created `External/Implementations/Mock/MockGovernmentDataGenerator.cs`, a small internal helper
   (not exposed via DI) that derives a deterministic fake vehicle profile (Make/Model/Year/
   FuelType/Colour) from a seeded `Random(hash(registrationNumber))`, shared by both mock adapters
   so they agree with each other for the same plate without depending on each other or on the
   caller's real vehicle record.
5. Created `MockDVLAAdapter.cs`/`MockDVSAAdapter.cs`. First pass generated Tax/MOT status
   independently of their due/expiry dates, which produced nonsensical combinations (e.g. "Taxed"
   next to a tax-due-date from 2021) - caught this before shipping and fixed by generating the
   dates first and deriving status from whether they're in the future relative to today.
6. Registered both as singletons in `Program.cs`, outside the LiteDB/Postgres conditional branch
   (government data adapters aren't storage-backend-specific).
7. Added `IDVLAAdapter`/`IDVSAAdapter` to both `APIController` and `VehicleController`'s constructor
   injection (existing fat-controller pattern).
8. Added `GET /api/vehicle/governmentdata?vehicleId=` (`Controllers/API/GovernmentDataController.cs`,
   `CollaboratorFilter`), returning a combined `VehicleGovernmentDataViewModel { DVLAData,
   MotHistory }`.
9. Added the same combined view model as a field on `ReportViewModel`, populated inline in
   `ReportController.GetReportPartialView` (the Dashboard/Report tab's existing synchronous
   render path - confirmed this tab isn't AJAX-lazy-loaded per-panel like Documents/Parts, every
   panel renders together in one call, so government data follows that same convention rather than
   introducing a new loading pattern).
10. Created `Views/Vehicle/Report/_GovernmentData.cshtml` and added it to `_Report.cshtml`'s
    existing panel grid. First real adoption of Phase 2's `.status-badge` primitive (flagged as
    unused in `DEFERRED.md` with "future MOT status" called out as a candidate use) for Tax status
    (green/neutral/red for Taxed/SORN/Untaxed) and MOT status (green/red for Valid/Not valid), and
    of `.ct-empty-state` for the no-license-plate case.
11. Verified via curl against the running app:
    - `GET /api/vehicle/governmentdata?vehicleId=1` (the user's real vehicle, read-only, no
      mutation) returned internally-consistent data (`Taxed`/`2027-03-29`, `Valid`/`2027-01-13`)
      after the status-derivation fix, with matching Make (`AUDI`)/Colour (`WHITE`)/FuelType
      (`HYBRID ELECTRIC`) between the DVLA and MOT-history responses.
    - Missing `vehicleId` → 400; nonexistent `vehicleId` → 404.
    - Created a throwaway vehicle (id 2, deleted after) with `identifier=VIN` and no `LicensePlate`
      - confirmed both `Found: false` on the API and the `.ct-empty-state` panel render on the
      Dashboard HTML.
    - Fetched the full rendered Dashboard/Report partial HTML for the real vehicle and confirmed
      the new panel's markup, badge classes, and MOT test/advisory list all render correctly with
      no stray whitespace or malformed nesting.
    - `dotnet build`: 0 errors, 224 warnings (unchanged from before this phase - no new warnings
      introduced).

## Result

Complete. `REQUIREMENTS.md` FR-GOV-01 satisfied with deterministic, clearly-labelled mock data,
curl-verified end-to-end (API + rendered HTML), real vehicle untouched (read-only throughout).
