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

## Increment 2: Shared sidebar shell, ported to Home/Index only

### Task packet

```
TASK ID: PHASE-16-02
TITLE: Build the shared sidebar nav-list partial and port Home/Index onto a real sidebar shell
OBJECTIVE: Replace Home/Index's horizontal top tab-strip with a persistent left sidebar matching the
  mockup's editorial small-caps style, using only existing design tokens - desktop only for this
  increment, mobile keeps today's hamburger+drawer behavior completely unchanged.
INPUTS: Views/Home/Index.cshtml (current nav construction), Views/Shared/_Layout.cshtml (Nav section
  slot, .lubelogger-body-container), wwwroot/css/site.css (existing tokens, .lubelogger-navbar-
  container's fixed-top-bar pattern as the positioning precedent to mirror for a fixed-left-sidebar),
  wwwroot/js/shared.js (bindNavBarResize/checkNavBarOverflow - the old horizontal-overflow-collapse
  logic that needed to be understood before deciding whether it still applied).
ALLOWED SCOPE: One new shared partial (Views/Shared/_SidebarNavList.cshtml); Home/Index.cshtml's Nav
  section rebuilt around it; new .ct-sidebar* CSS classes reusing only existing tokens; removing the
  now-dead bindNavBarResize() call from Home/Index specifically (not from shared.js itself, which
  Vehicle/Index still needs unchanged until Increment 3).
NON-SCOPE: Vehicle/Index (Increment 3); any change to _Layout.cshtml's DOM structure (achieved via a
  CSS :has() selector instead, scoped so Login/Kiosk/Admin/unported pages are provably unaffected);
  mobile sidebar/drawer redesign (mobile keeps exactly today's behavior this round).
IMPLEMENTATION REQUIREMENTS:
  - _SidebarNavList.cshtml: a dumb partial over List<(string Id, string Target, string Icon, string
    Label)>, rendering the same data-bs-toggle="tab"/data-bs-target markup pattern already in use -
    zero change to how Bootstrap's tab JS or garage.js finds/activates panes, since every tab-pane id
    stays exactly as it was.
  - .ct-sidebar: position:fixed, left:0, fixed width, full height, reusing --ct-space-*/--bs-border-
    color/--bs-body-bg tokens - no new color values. Active-state indicator is a left border accent
    bar (--ct-spotlight-solid) rather than the mockup's dot bullet - chosen to match this app's flat/
    sharp-corner ethos (docs/UI_SPEC.md's "Zara's single most load-bearing structural trait") rather
    than introduce a new circular motif with no precedent anywhere else in the design system. Flagged
    for the user's live review - easy to swap for a literal dot if preferred.
  - Body-container offset via `body:has(.ct-sidebar) .lubelogger-body-container { padding-left: ... }`
    scoped inside a `min-width: 576px` media query, rather than touching _Layout.cshtml at all - kept
    every other page (Login/Kiosk/Admin/not-yet-ported Vehicle/Index) provably unaffected without
    needing to audit all of them individually.
  - Mobile (<576px): a new minimal .ct-mobile-topbar (wordmark + hamburger button only, reusing the
    existing .lubelogger-navbar-container fixed-positioning class) replaces the old full tab-strip top
    bar; the existing .lubelogger-mobile-nav full-screen drawer and its showMobileNav()/hideMobileNav()
    JS are completely untouched.
  - Real bug caught before shipping, not by inspection alone: bindNavBarResize() sets a ResizeObserver
    on .lubelogger-navbar and, via checkNavBarOverflow(), measures `.lubelogger-navbar > .lubelogger-
    tab > .nav-item .bi` width/font-size to detect icon-font-not-loaded-yet and retry. With the sidebar
    in place, Home/Index's .lubelogger-navbar (now mobile-topbar-only) has no .lubelogger-tab children
    left, so that selector always returns an empty set - iconWidth becomes the literal string
    "undefinedpx" and iconFontSize becomes undefined, which never satisfy the loop's exit condition,
    causing checkNavBarOverflow() to re-queue itself via setTimeout every 500ms forever. Fixed by
    removing the bindNavBarResize() call from Home/Index's own bottom script block (the function
    itself, and Vehicle/Index's call to it, are untouched - still needed there until Increment 3).
DELIVERABLES: A working sidebar on Home/Index, verified structurally (not just by code reading).
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - GET /Home returns the sidebar markup (.ct-sidebar, .ct-sidebar-header, .ct-sidebar-nav links with
    correct ids) and every original tab-pane id unchanged.
  - No leftover .nav-item-more (overflow dropdown) markup remains.
  - No Razor rendering errors leaked into the response body.
  - /css/site.css serves with balanced braces (sanity check that the new CSS block didn't corrupt the
    file) and contains the new .ct-sidebar rules.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Home | grep -o 'class="ct-sidebar[^"]*"'
  curl http://localhost:5300/Home | grep -o 'id="[a-z]*-tab-pane"'   (confirm unchanged)
  curl http://localhost:5300/Home | grep -c "nav-item-more"          (confirm 0)
  curl http://localhost:5300/css/site.css                            (brace-balance sanity check)
STOP CONDITION: All structural checks green. The user must do the actual visual review before
  Increment 3 (porting Vehicle/Index onto the same shell) starts - no browser tool exists here to
  verify layout/spacing/readability, only markup/CSS presence.
```

### What was done

1. Read `Views/Shared/_Layout.cshtml` first to confirm every page in the app shares one layout (no
   per-area override anywhere - `Views/_ViewStart.cshtml` sets `Layout = "_Layout"` unconditionally),
   which is why the `:has()`-scoped CSS approach was chosen over restructuring `_Layout.cshtml`
   itself: a shared-file change would have needed auditing Login/Kiosk/Admin for regressions too,
   while a selector scoped to "only when `.ct-sidebar` is actually present" provably can't affect
   pages that don't render one.
2. Read `wwwroot/css/site.css`'s existing `.lubelogger-navbar-container` (`position:fixed; top:0;
   left:0`) as the positioning precedent for the new fixed-left `.ct-sidebar`, and confirmed
   `--ct-space-*`/`--bs-border-color`/`--bs-body-bg`/`--ct-spotlight-solid` were all it needed - no
   new tokens.
3. Built `Views/Shared/_SidebarNavList.cshtml` and rebuilt `Home/Index.cshtml`'s `@section Nav`
   around it: a desktop `.ct-sidebar` (wordmark header, vertical nav list via the shared partial twice
   - once for the main tabs, once for the Settings tab which was previously positioned separately via
   `ms-auto` - now just a second call with a one-item list) plus a user/admin dropdown pinned to the
   sidebar footer, and a separate minimal `.ct-mobile-topbar` for <576px that only triggers the
   pre-existing, untouched `.lubelogger-mobile-nav` drawer.
4. Simplified the wordmark/logo swap: the original had a small-logo/large-logo pair swapped via CSS
   classes tied to the old horizontal-bar's width constraints (`.lubelogger-tab` vs
   `.lubelogger-mobile-nav-show`). A sidebar has no such width pressure, so both the sidebar header and
   the mobile topbar now just show one appropriately-sized logo each - a deliberate simplification, not
   an oversight, since the reason for the original dual-swap no longer applies.
5. Read `wwwroot/js/shared.js`'s `bindNavBarResize()`/`checkNavBarOverflow()` before deciding whether
   Home/Index still needed them, and found a real bug that would have shipped otherwise: with no
   `.lubelogger-tab` children left in Home's `.lubelogger-navbar` (now mobile-topbar-only), the
   function's icon-width/font-size comparison would always disagree (`"undefinedpx" != undefined`),
   causing an infinite `setTimeout(...,500)` retry loop on every page load - a real runaway-timer bug,
   not a hypothetical. Fixed by removing the `bindNavBarResize();` call from Home/Index's own script
   block (kept `bindWindowResize();`, which is still relevant and unaffected). Left the function itself
   and Vehicle/Index's identical call to it untouched, since Vehicle/Index still needs the real
   overflow-collapse behavior until Increment 3 ports it too.
6. Verified structurally, not just by reading the code: `dotnet build` (0 errors), `dotnet test` (10/10
   passing), started the dev instance (port 5300 - 5299 is the Phase 15 production service), and
   curled the actual rendered output: `.ct-sidebar`/`.ct-sidebar-header` present, nav links resolve to
   the same `garage-tab`/`calendar-tab`/`settings-tab` ids as before (`supply-tab` correctly absent in
   this dev config, matching the pre-existing `GetServerEnableShopSupplies()` conditional - not a
   regression), all four original `*-tab-pane` ids unchanged, zero leftover `.nav-item-more` markup,
   zero leaked Razor errors in the response body, and `/css/site.css` served with balanced braces
   (424/424) containing the 10 new `.ct-sidebar*` rule occurrences.
7. One design choice made without the user's input, flagged explicitly for their review rather than
   guessed at silently: the mockup's active-nav-item indicator is a small dot bullet; this
   implementation uses a left-border accent bar instead, reasoned from `docs/UI_SPEC.md`'s explicit
   "flat/sharp corners governs everything" framing rather than introducing a circular motif with no
   precedent elsewhere in the design system. Easy to change if the user prefers the literal dot after
   seeing it live.

### Result

Structurally complete and verified as far as this environment allows (build, tests, curl-inspected
markup/CSS). **Not yet visually verified** - no browser/screenshot tool exists here. The user needs to
load the app live (desktop and mobile) before Increment 3 (porting `Vehicle/Index` onto the same
shell) starts.
