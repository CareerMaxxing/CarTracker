# PHASE_16 — Sidebar App Shell & Dashboard Redesign

New phase. The user shared a dashboard mockup ("DriveLog") whose editorial aesthetic — serif
headlines, flat corners, warm paper/ink palette — is close in spirit to this app's own already-
finished "Zara + Magneto" design system (`wwwroot/css/site.css`, `docs/UI_SPEC.md`), but its actual
*structure* is different: a persistent left sidebar with "Dashboard" as its own single-vehicle-focused
landing page, separate from a "Vehicles" list — versus this app's current top tab-strip with an
all-vehicles Garage grid as the homepage. User confirmed the full structural change, intentionally
reversing a decision `docs/execution/UI_TRANSITION.md` made on purpose ("Home/Index and Vehicle/Index
nav stays unshared... a deliberate scope boundary, not an oversight").

Scope for this phase, per the user's explicit choice when asked: a **visual/layout pass using only
real, already-existing data**. Several mockup widgets (Trips, a fuel-tank/range gauge, month-over-
month cost %, fleet-wide all-time stats) have no backing data concept in this app at all — flagged as
candidate future phases, not designed in detail here.

Full plan (context, the two anchoring mechanism decisions, all 11 increments, nav-item mapping,
explicit out-of-scope list) is preserved in the approved plan at the time this phase started;
increments below are documented as each is actually completed, matching this project's established
Phase 14/15 pattern. **No browser/screenshot tool exists in this environment** - every increment gets
build+test+curl verification from the agent, but real visual review needs the user looking at it live
in a browser, same as the original Zara + Magneto work.

## Increment 1: `CurrentVehicleId` plumbing (no UI change)

### Task packet

```
TASK ID: PHASE-16-01
TITLE: Add a per-user "current vehicle" concept, mirroring the existing DefaultTab pattern
OBJECTIVE: Give the app a way to remember which vehicle a user's Dashboard should currently show,
  as pure plumbing with zero visual/behavioral change yet - later increments (4+) will actually
  consume it to route the landing page and drive a vehicle-switcher.
INPUTS: Models/Settings/UserConfig.cs (DefaultTab field/pattern), Helper/ConfigHelper.cs
  (GetUserConfig's CheckString/int.Parse round-trip at line 489, SaveUserConfig's whole-object
  serialization), Controllers/HomeController.cs's existing WriteToSettings action as the convention
  to match for a small dedicated setter.
ALLOWED SCOPE: One new UserConfig field; one new read-side line in ConfigHelper.GetUserConfig; one
  new HomeController action. No resolver/fallback logic yet (nothing consumes the field until
  Increment 4, where the fallback-to-first-accessible-vehicle logic belongs) - keeping this increment
  minimal and reviewable on its own, not half-building a consumer with nothing to call it yet.
NON-SCOPE: Any UI (sidebar, vehicle switcher) - Increments 2-4. Any resolver/fallback logic - deferred
  to Increment 4 where it's actually used.
IMPLEMENTATION REQUIREMENTS:
  - Models/Settings/UserConfig.cs: `public int CurrentVehicleId { get; set; }` next to `DefaultTab`.
  - Helper/ConfigHelper.cs's GetUserConfig: `CurrentVehicleId = int.Parse(CheckString(nameof(UserConfig.CurrentVehicleId), "0")),`
    - exact same pattern as DefaultTab, minus the enum cast.
  - SaveUserConfig needed NO changes - confirmed by reading it first: it already serializes whatever
    whole UserConfig object it's given, for both the root-user file path and the per-collaborator DB
    path. Assuming a write-side change was needed (as the original plan draft guessed) would have
    been extra unnecessary work.
  - Controllers/HomeController.cs: new `[HttpPost] SetCurrentVehicle(int vehicleId)` action - loads
    existing config, sets the one field, saves - matching WriteToSettings' existing
    load-mutate-save shape exactly, but scoped to one field instead of binding a whole settings form.
DELIVERABLES: A working, persisted CurrentVehicleId field with a settable endpoint, verified via a
  real round-trip (not just code-reading).
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors.
  - dotnet test: 10/10 passing, no regression.
  - POST /Home/SetCurrentVehicle?vehicleId=1 returns true and persists "CurrentVehicleId":1 into
    data/config/userConfig.json.
  - Resetting to vehicleId=0 round-trips correctly too (confirms the default-value path, not just the
    happy path).
  - An unrelated existing page (Garage) still loads normally - confirms no regression from the new
    field being present in every GetUserConfig call site.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background - port 5300, not 5299, since the
  production Windows Service now owns 5299)
  curl -X POST http://localhost:5300/Home/SetCurrentVehicle -d "vehicleId=1"
  grep CurrentVehicleId data/config/userConfig.json
  curl http://localhost:5300/Home/Garage (regression check)
STOP CONDITION: All validation commands green, real round-trip confirmed (not inferred from code
  reading alone), before starting Increment 2 (the actual sidebar shell - the highest structural risk
  increment in this phase, deliberately sequenced after the safest possible first step).
```

### What was done

1. Added `CurrentVehicleId` to `Models/Settings/UserConfig.cs`, directly beside `DefaultTab` (the
   pattern being mirrored).
2. Added the read-side line to `Helper/ConfigHelper.cs`'s `GetUserConfig`, matching `DefaultTab`'s
   exact `int.Parse(CheckString(...))` shape minus the enum cast.
3. Read `SaveUserConfig` before touching it and found it needed no changes at all - it already
   serializes the whole `UserConfig` object it's handed, for both the root-user JSON file and the
   per-collaborator DB path. The original plan draft had guessed a write-side change might be needed;
   confirmed unnecessary by reading the actual method first rather than assuming symmetry with the
   read side.
4. Added `HomeController.SetCurrentVehicle(int vehicleId)`, matching the existing `WriteToSettings`
   action's load-existing-config → mutate → `SaveUserConfig` shape, scoped to just this one field.
5. Verified for real, not just by reading code: started the dev instance on port 5300 (5299 is now
   the production Windows Service from Phase 15 - reused that port-separation convention rather than
   colliding with it), `POST /Home/SetCurrentVehicle -d "vehicleId=1"` returned `true` and
   `data/config/userConfig.json` showed `"CurrentVehicleId":1`; reset to `vehicleId=0` and confirmed
   that round-trips correctly too (the default-value path, not just the happy path); confirmed an
   unrelated existing page (`/Home/Garage`) still returns `200` - no regression from the new field
   being present in every `GetUserConfig` call site across the app.
6. `dotnet build`: 0 errors. `dotnet test Tests/CarCareTracker.Tests.csproj`: 10/10 passing.

### Result

Complete. Pure plumbing, zero visual change, verified via a real HTTP round-trip against the running
dev instance rather than trusted from code reading alone. Increment 2 (the shared sidebar shell,
ported to `Home/Index` first) is next - the highest structural risk increment in this phase, and
needs the user's live browser review since no screenshot tool exists in this environment.
