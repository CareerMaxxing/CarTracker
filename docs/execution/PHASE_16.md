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

User reviewed live, said "better, carry on" - read as approval to proceed, not a request for specific
changes (none were named). Moved on to Increment 3.

## Increment 3: Port Vehicle/Index onto the same shell

Split from the original plan's Increment 3, which bundled the nav port together with a new
vehicle-switcher feature. Given `Vehicle/Index`'s nav is meaningfully more complex than `Home/Index`'s
(13 tabs, drag-and-drop `TabOrder`, a live-JS reminder-bell icon, per-tab visibility via
`VisibleTabs`), the port alone was already a full increment's worth of risk - bundling a second new
feature (the switcher, which needs its own new controller endpoint + dropdown UI) would have made one
already-complex change harder to verify and roll back independently. Re-sequenced as 3 (this entry, nav
port only) and 3b (vehicle switcher, next) rather than expanding scope - not a new decision needing the
user's sign-off, just safer ordering within already-approved work.

### Task packet

```
TASK ID: PHASE-16-03
TITLE: Port Vehicle/Index onto the shared sidebar shell, preserving TabOrder/VisibleTabs/reminder-bell
OBJECTIVE: Replace Vehicle/Index's horizontal top tab-strip with the same .ct-sidebar pattern
  Increment 2 built for Home/Index, without losing any of its richer per-item behavior.
INPUTS: Views/Vehicle/Index.cshtml (vehicleNavTabs list, TabOrder-driven CSS order, DefaultActiveTab
  visibility gating, the reminder-bell special icon, Parts/Documents/Search extra items),
  Views/Shared/_SidebarNavList.cshtml (Increment 2's partial - too simple as-is for this view's needs),
  wwwroot/js/vehicle.js (getVehicleHaveImportantReminders - confirms .reminderBell/.reminderBellDiv
  are unscoped jQuery class hooks, not id-scoped, so they just need to exist somewhere in the DOM).
ALLOWED SCOPE: Widening _SidebarNavList.cshtml's model to carry three more per-item values
  (CssClass, Style, IsReminderBell) so ONE partial still serves both views, rather than forking a
  second near-duplicate partial; Vehicle/Index.cshtml's Nav section rebuilt around it; Home/Index's
  two existing partial calls updated to the new tuple shape (empty string/false for the fields it
  doesn't need); removing Vehicle/Index's own now-dead bindNavBarResize() call.
NON-SCOPE: The vehicle-switcher (split out as 3b); mobile drawer redesign (kept exactly as today,
  same as Increment 2's approach for Home/Index).
IMPLEMENTATION REQUIREMENTS:
  - _SidebarNavList.cshtml's model becomes List<(string Id, string Target, string Icon, string Label,
    string CssClass, string Style, bool IsReminderBell)>. CssClass carries StaticHelper.
    DefaultActiveTab(userConfig, tab.Mode)'s result directly - despite its name this returns "d-none"
    when the tab isn't in the user's VisibleTabs, not an "active" indicator; a fresh vehicle defaults
    to only Dashboard visible (see STATE.md's standing note), so this MUST be preserved exactly, not
    dropped as dead code. Style carries `order: N` (TabOrder.FindIndex), which works identically for
    flexbox column direction as it did for the old row direction - no logic change, just re-verified
    it still applies in a vertical list. IsReminderBell swaps the plain `<i class="bi @tab.Icon">` for
    the `<div class="reminderBellDiv"><i class="reminderBell bi bi-bell">` wrapper the live urgency-
    polling JS targets - confirmed via vehicle.js that these are unscoped jQuery selectors, so exactly
    one instance existing in the visible sidebar is sufficient (was 3 duplicate-id copies before across
    desktop/dropdown/mobile; now 2 - sidebar + mobile drawer - an incidental reduction, not a targeted
    fix).
  - The Search item has no data-bs-toggle/target at all (calls showGlobalSearch() directly) so it
    can't go through the tab-triggering partial - kept as a standalone <li> outside the loop, same
    pattern Increment 2 used for Home's Settings-tab special positioning.
  - "Edit Vehicle" (previously a top-bar pencil icon) moved into a new .ct-sidebar-footer button,
    reusing Increment 2's footer class rather than inventing a new position for it.
  - Home/Index.cshtml's two _SidebarNavList calls updated for the new 7-field tuple (LINQ projection
    adding "", "", false per item) - the only change needed there since its own list stayed simple.
DELIVERABLES: A working sidebar on Vehicle/Index with all 15 tabs (13 + Parts + Documents) plus a
  separate Search item, verified structurally.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - GET /Vehicle/Index?vehicleId=1 (the real vehicle) returns all 15 sidebar nav-link ids in
    TabOrder-derived sequence, all 15 original tab-pane ids unchanged, the reminder-bell wrapper
    present exactly once, search-tab present with its onclick handler intact, zero d-none items (this
    real vehicle has every tab visible - confirms VisibleTabs gating still evaluates correctly, not
    just that the CssClass field exists), zero leftover .nav-item-more, zero leaked Razor errors.
  - /css/site.css still parses with balanced braces after Increment 2's rules are joined by nothing
    new (this increment added no new CSS - the existing .ct-sidebar* rules already cover both views).
  - Home/Index (GET /Home/Garage) still loads normally - confirms the tuple-shape widening didn't
    regress the already-shipped Increment 2 page.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl "http://localhost:5300/Vehicle/Index?vehicleId=1" | grep -o 'ct-sidebar-link" id="[a-z-]*"'
  curl "http://localhost:5300/Vehicle/Index?vehicleId=1" | grep -o 'id="[a-z]*-tab-pane"' | sort -u | wc -l
  curl "http://localhost:5300/Vehicle/Index?vehicleId=1" | grep -o 'reminderBellDiv[^<]*<i class="reminderBell bi bi-bell">'
  curl http://localhost:5300/Home/Garage
STOP CONDITION: All structural checks green against the real vehicle's data, not a synthetic/empty
  one. User visual review still required before 3b (vehicle switcher) and Increment 4 start.
```

### What was done

1. Read `wwwroot/js/vehicle.js`'s `getVehicleHaveImportantReminders` before assuming anything about
   the reminder-bell markup's requirements - confirmed `.reminderBell`/`.reminderBellDiv` are plain,
   unscoped jQuery class selectors (not tied to any specific parent id), so the wrapper just needs to
   exist once in whichever nav list is actually visible - not something requiring special per-context
   duplication logic.
2. Read `Helper/StaticHelper.cs`'s `DefaultActiveTab` before treating it as an "active tab" concept (as
   its name suggested) - it actually returns `"d-none"` when a tab isn't in the user's `VisibleTabs`,
   a real per-vehicle customization (a fresh vehicle defaults to only Dashboard visible, per this
   project's own standing STATE.md note) that would have silently broken if dropped as apparent
   dead weight.
3. Widened `_SidebarNavList.cshtml`'s model from 4 to 7 tuple fields (`CssClass`, `Style`,
   `IsReminderBell` added) rather than forking a second near-duplicate partial for Vehicle/Index -
   keeps the "one shared dumb partial" plan intent intact even though Vehicle's per-item data is
   richer than Home's.
4. Updated `Home/Index.cshtml`'s two existing partial calls (from Increment 2) to the new tuple shape
   via a LINQ projection - the only change needed on that already-shipped page.
5. Rebuilt `Vehicle/Index.cshtml`'s `@section Nav`: a `.ct-sidebar` header showing the vehicle
   thumbnail/wordmark plus the vehicle's year/make/model title, the 15-item nav list via the widened
   partial (13 original tabs + Parts + Documents, each carrying its real `DefaultActiveTab`/`order`/
   reminder-bell values), a standalone Search item outside the partial (no tab target to toggle), and
   "Edit Vehicle" moved into the sidebar footer. Mobile keeps its own separate minimal top bar
   (mirroring Increment 2's `.ct-mobile-topbar` pattern) triggering the completely untouched
   `.lubelogger-mobile-nav` drawer.
6. Removed `bindNavBarResize();` from Vehicle/Index's own bottom script block - the same infinite-
   retry-loop risk Increment 2 found and fixed for Home/Index applies here too, for the identical
   reason (no `.lubelogger-tab` children left in the desktop `.lubelogger-navbar` for it to measure).
7. Verified structurally against the real vehicle (id=1, the actual BMW Z4, not a synthetic test
   record): `dotnet build` (0 errors), `dotnet test` (10/10 passing), curled
   `/Vehicle/Index?vehicleId=1` and confirmed all 15 sidebar nav-link ids present in the correct
   TabOrder-derived sequence, all 15 original tab-pane ids unchanged, the reminder-bell wrapper present
   exactly once, `search-tab` present with its `onclick` intact, zero `d-none` items (this real
   vehicle's `VisibleTabs` includes everything, confirming the gating logic still evaluates - not just
   that the field exists structurally), zero leftover `.nav-item-more`, zero leaked Razor errors, and
   the vehicle title rendering correctly as "2004 BMW Z4". Also re-confirmed `/Home/Garage` (Increment
   2's page) still loads normally after the shared partial's model change, and `/css/site.css` still
   parses with balanced braces.

### Result

Structurally complete and verified against real data, same caveat as Increment 2: **not yet visually
verified**. 3b (vehicle-switcher) and Increment 4 (landing page routing to the current vehicle's
Dashboard) both need this port confirmed working live first.

User reviewed live, said "bang on, continue" - clear approval. Moved on to 3b.

## Increment 3b: Vehicle switcher

### Task packet

```
TASK ID: PHASE-16-03B
TITLE: Add a vehicle-switcher dropdown to Vehicle/Index's sidebar header
OBJECTIVE: Let a multi-vehicle household switch which vehicle they're viewing directly from the
  sidebar, using Increment 1's CurrentVehicleId endpoint, without duplicating vehicle-list-fetching
  logic that already exists elsewhere in this codebase.
INPUTS: Controllers/HomeController.cs's existing GetVehicleSelector action (the "duplicate to other
  vehicle" feature - same GetVehicles + FilterUserVehicles + HideSoldVehicles filtering pattern
  needed here, but for a different UX: single-select navigation, not multi-select checkboxes) and its
  Views/Home/_VehicleSelector.cshtml partial (studied as the pattern to follow, not reused directly -
  its checkbox-list UI doesn't fit a single-select switcher).
ALLOWED SCOPE: One new HomeController action (GetVehicleSwitcherList) reusing the exact same
  filtering as GetVehicleSelector minus its vehicleId-exclusion and ShopSupplies special-casing
  (both specific to the duplicate-record use case, not relevant here); one new partial
  (_VehicleSwitcherList.cshtml); two new shared.js functions (loadVehicleSwitcher - lazy AJAX fetch on
  first dropdown open, switchToVehicle - POSTs Increment 1's SetCurrentVehicle then navigates);
  Vehicle/Index.cshtml's sidebar header restructured to separate the existing "click wordmark to
  return to Garage" area from the new switcher dropdown (they can't share one onclick handler).
NON-SCOPE: Adding the switcher to Home/Index (Home isn't vehicle-scoped yet - that's Increment 4's
  job); any change to the existing GetVehicleSelector/duplicate-to-vehicle feature.
IMPLEMENTATION REQUIREMENTS:
  - GetVehicleSwitcherList: _dataAccess.GetVehicles() -> FilterUserVehicles (non-root users only) ->
    HideSoldVehicles removal if set - identical filtering to GetVehicleSelector, deliberately not
    excluding the current vehicle (a switcher should show where you are, not just where you could go).
  - _VehicleSwitcherList.cshtml: one <button class="dropdown-item" onclick="switchToVehicle(id)">
    per vehicle, labeled the same way the existing selector already does (Year Make Model
    (Identifier), via StaticHelper.GetVehicleIdentifier) for label consistency with the rest of the
    app rather than inventing new formatting.
  - loadVehicleSwitcher(): guarded by a module-level `vehicleSwitcherLoaded` flag so opening the
    dropdown twice doesn't re-fetch: fetches once, populates #vehicleSwitcherMenu, done. Triggered by
    the toggle button's own onclick (fires before Bootstrap's dropdown show, both attached to the same
    click) rather than a show.bs.dropdown listener, since it's simpler for a single call site.
  - switchToVehicle(vehicleId): POST to /Home/SetCurrentVehicle, then on success navigate to
    /Vehicle/Index?vehicleId={id} - the endpoint call and the navigation are sequenced (navigate only
    in the POST's success callback) so the stored CurrentVehicleId is guaranteed to be updated before
    the page that will eventually read it (Increment 4) ever loads.
  - Vehicle/Index.cshtml's header split into two siblings under .ct-sidebar-header: the existing
    .ct-wordmark-wrap (thumbnail/wordmark + vehicle title, onclick="returnToGarage()", unchanged
    behavior) and a new sibling .dropdown containing the switcher toggle + menu - avoids stacking two
    conflicting onclick handlers on one element.
DELIVERABLES: A working vehicle switcher, verified against the real household's actual vehicle list
  (not a single-vehicle or synthetic test case).
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - GET /Home/GetVehicleSwitcherList returns real vehicle labels for every vehicle in the account, not
    an empty/placeholder list.
  - GET /Vehicle/Index?vehicleId=1 contains the switcher's toggle button and an initially-empty
    #vehicleSwitcherMenu (populated client-side on open, not server-rendered - consistent with this
    codebase's dominant AJAX-loaded-dropdown pattern).
  - POST /Home/SetCurrentVehicle still round-trips correctly (regression check - confirms the
    Increment 1 endpoint this feature depends on wasn't disturbed).
  - /Home/Garage still loads normally (regression check on the unrelated Home page).
  - /css/site.css still parses with balanced braces (no new CSS was needed this increment - the
    existing .ct-sidebar-link class already covers the switcher toggle's styling).
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Home/GetVehicleSwitcherList
  curl "http://localhost:5300/Vehicle/Index?vehicleId=1" | grep -o 'id="vehicleSwitcherMenu"'
  curl -X POST http://localhost:5300/Home/SetCurrentVehicle -d "vehicleId=1"
  curl http://localhost:5300/Home/Garage
STOP CONDITION: All structural checks green, the switcher's own AJAX endpoint confirmed returning the
  real household's actual vehicles (discovered along the way: there are two - a BMW Z4 and a Volvo
  S80 - the first real multi-vehicle test case this phase has had). User visual review (does opening
  the dropdown and clicking a different vehicle actually switch pages correctly) still required before
  Increment 4.
```

### What was done

1. Read the existing `GetVehicleSelector` action and `_VehicleSelector.cshtml` first, since this
   feature ("pick a vehicle") already exists in the app for a different purpose (bulk-duplicating a
   record to other vehicles) - confirmed its checkbox-list UI didn't fit a single-select switcher, but
   its filtering logic (`GetVehicles` → `FilterUserVehicles` for non-root users → `HideSoldVehicles`
   removal) was exactly right to reuse, rather than re-deriving vehicle-access rules from scratch.
2. Added `HomeController.GetVehicleSwitcherList()`, same filtering, deliberately not excluding the
   current vehicle (unlike the duplicate-to-vehicle feature, which excludes the source vehicle since
   duplicating a record to itself is meaningless - a switcher needs the opposite: show where you
   currently are too).
3. Added `Views/Home/_VehicleSwitcherList.cshtml`, reusing `StaticHelper.GetVehicleIdentifier`'s
   "Year Make Model (Identifier)" label format for consistency with the existing selector rather than
   inventing new formatting.
4. Added `loadVehicleSwitcher()`/`switchToVehicle()` to `shared.js` (available to both views, though
   only wired into Vehicle/Index this round) - the switch function sequences the `SetCurrentVehicle`
   POST and the page navigation deliberately (navigate only in the success callback) so Increment 4's
   later read of `CurrentVehicleId` can never race against an in-flight save.
5. Restructured `Vehicle/Index.cshtml`'s sidebar header into two siblings (existing wordmark/title
   click-to-Garage area, new switcher dropdown) rather than trying to attach two different onclick
   behaviors to one element.
6. Verified structurally: `dotnet build` (0 errors), `dotnet test` (10/10 passing), curled
   `/Home/GetVehicleSwitcherList` directly and got back real data - this household actually has two
   vehicles (a BMW Z4 and a Volvo S80), the first genuine multi-vehicle case this phase has exercised,
   not just inferred from account structure; confirmed the switcher markup renders on
   `/Vehicle/Index?vehicleId=1`; re-confirmed `SetCurrentVehicle` still round-trips (this increment
   depends on Increment 1's endpoint, worth re-checking it wasn't disturbed); confirmed `/Home/Garage`
   unaffected; confirmed `/css/site.css` still balanced (no new CSS needed - the switcher toggle reuses
   `.ct-sidebar-link` as-is).

### Result

Structurally complete, verified against the real household's actual two-vehicle list. **Not yet
visually verified** - specifically, whether clicking a different vehicle in the dropdown actually
navigates and lands on the right page has not been exercised end-to-end by this agent (curl confirms
the pieces work independently, not the full click-through). User review needed before Increment 4
(landing page routing to the current vehicle's Dashboard).

User said "continue" without explicitly confirming the click-through - read as approval to proceed.
Moved on to Increment 4, the first increment that actually consumes CurrentVehicleId rather than just
plumbing/setting it, so extra care was taken here.

## Increment 4: Landing page routes to the current vehicle's Dashboard

### Task packet

```
TASK ID: PHASE-16-04
TITLE: Make / and /Home land on the current vehicle's Dashboard instead of the all-vehicles Garage
  grid, with an explicit, discoverable way back to the grid
OBJECTIVE: Complete the mockup's IA - "Dashboard" as the default landing experience, "Vehicles" as a
  deliberate, separate destination - using the CurrentVehicleId plumbing and vehicle switcher already
  built in Increments 1-3b.
INPUTS: Controllers/HomeController.cs's Index() action (currently a bare `return View()`), the
  filtering pattern already established twice (GetVehicleSelector, GetVehicleSwitcherList), all
  callers of the shared returnToGarage() JS function (found by grepping the whole wwwroot tree, not
  assumed to be Vehicle/Index-only).
ALLOWED SCOPE: HomeController.Index() gains a resolve-and-redirect branch; returnToGarage() gains a
  query parameter to explicitly opt out of the new redirect; one new "Vehicles" sidebar item in
  Vehicle/Index's footer for discoverability (previously only reachable by clicking the wordmark, not
  labeled as a distinct action).
NON-SCOPE: Any change to _GarageDisplay.cshtml's own empty-state handling (already exists, untouched,
  still reachable exactly as before once showGarage=true is passed); changing login.js's or
  vehicle.js's own bare-/Home redirects (deliberately left alone - see reasoning below).
IMPLEMENTATION REQUIREMENTS:
  - A real, easy-to-miss design trap found and solved before writing any code: if `/Home` always
    redirects to a vehicle whenever one exists, there would be NO way to ever reach the Garage grid
    again - every link back to "/Home" (the wordmark click, the mobile drawer's Garage item, both
    already wired to returnToGarage()) would just bounce straight back into a vehicle redirect loop.
    Solved with an explicit `showGarage` query flag: `Index(bool showGarage = false)` skips the
    redirect entirely when true; `returnToGarage()` was updated to pass it
    (`/Home?showGarage=true`), which is the ONLY call site that needed to change - the mobile drawer's
    Garage item already called returnToGarage() and inherited the fix automatically, no separate edit
    needed there.
  - Grepped the whole wwwroot tree for every other place that navigates to a bare '/Home' before
    deciding what NOT to touch: login.js's post-login redirect and vehicle.js's post-delete-vehicle
    redirect both stay bare (no showGarage=true) - both are cases where landing on the (new) dashboard-
    first experience is the correct behavior, not an oversight. Deleting a vehicle naturally falls
    through to whichever vehicle remains (CurrentVehicleId won't match the deleted id, so the resolver
    falls back to the first remaining one) or the empty-state Garage view if none remain - handled
    entirely by the existing fallback logic already in the redirect, no special-casing needed for the
    delete flow specifically.
  - Index()'s resolver reuses the exact filtering pattern from GetVehicleSelector/
    GetVehicleSwitcherList (GetVehicles -> FilterUserVehicles for non-root -> HideSoldVehicles removal)
    for the third time now - deliberately not extracted into a shared helper method in this pass (three
    call sites with slightly different follow-on logic each), left as a candidate refactor if a fourth
    call site appears.
  - Picks userConfig.CurrentVehicleId if it's still in the accessible/filtered list, else the first
    accessible vehicle, else (empty list) falls through to `return View()` - the pre-existing Garage
    empty-state, untouched.
DELIVERABLES: Working landing-page redirect, verified against multiple real scenarios, not just the
  happy path.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - GET /Home (plain) returns 302 to /Vehicle/Index?vehicleId={the real current vehicle}.
  - GET /Home?showGarage=true returns 200 and renders the Garage tab-strip normally, not a redirect.
  - Setting CurrentVehicleId to the OTHER real vehicle (this household has two) and reloading /Home
    redirects to that specific vehicle, not just "some" vehicle - confirms the stored preference is
    actually respected, not coincidentally always picking the same one.
  - Setting CurrentVehicleId to a nonexistent id and reloading /Home falls back to the first accessible
    vehicle without erroring - confirms the fallback path, not just the happy path.
  - The new "Vehicles" sidebar item is present in Vehicle/Index's rendered HTML.
  - /Home/Garage (the AJAX partial) and /css/site.css (brace balance) both still fine - regression
    checks on already-shipped pages.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl -i http://localhost:5300/Home | head -5                              (expect 302 + Location)
  curl -o /dev/null -w "%{http_code}" "http://localhost:5300/Home?showGarage=true"   (expect 200)
  curl -X POST http://localhost:5300/Home/SetCurrentVehicle -d "vehicleId=2"
  curl -i http://localhost:5300/Home | grep -i location                     (expect vehicleId=2)
  curl -X POST http://localhost:5300/Home/SetCurrentVehicle -d "vehicleId=999"
  curl -i http://localhost:5300/Home | grep -i location                     (expect fallback, no 500)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1 | grep -o 'bi-car-front'
  curl http://localhost:5300/Home/Garage
  curl http://localhost:5300/css/site.css
STOP CONDITION: All four redirect scenarios (default, explicit-garage-bypass, respects-preference,
  invalid-id-fallback) verified independently via curl before considering this done - this is the
  first increment that actually changes what happens when someone opens the app, so the happy path
  alone wasn't enough confidence.
```

### What was done

1. Before writing any redirect code, worked out (and nearly missed) that an unconditional redirect
   would trap users - every path back to "/Home" would just bounce forward again, with no way to ever
   see the Garage grid. Solved with an explicit `showGarage` opt-out flag rather than any cleverer
   heuristic (referrer sniffing, session flags) that would have been more fragile.
2. Grepped the whole `wwwroot` tree for every existing bare `/Home` navigation before touching
   anything, rather than assuming `returnToGarage()` was the only call site: found two more
   (`login.js`'s post-login redirect, `vehicle.js`'s post-delete-vehicle redirect) and deliberately
   left both unchanged - both are cases where landing on the new dashboard-first experience is
   correct, not a gap.
3. Implemented `HomeController.Index(bool showGarage = false)`: resolves the current vehicle using the
   same three-part filtering pattern already established in `GetVehicleSelector` and
   `GetVehicleSwitcherList` (this codebase's third use of it now - noted as a refactor candidate if a
   fourth appears, not extracted yet since the three call sites' follow-on logic still differs enough
   to make a shared helper premature).
4. Updated `returnToGarage()` (the one function that needed to change) to pass `showGarage=true` -
   the mobile drawer's own Garage link already calls this same shared function, so it was fixed for
   free without a separate edit.
5. Added a new, explicitly labeled "Vehicles" sidebar item to `Vehicle/Index`'s footer (previously
   only reachable by clicking the wordmark/vehicle-title, an affordance that isn't obviously
   "click here to see all vehicles" without already knowing the app).
6. Verified against real, deliberately adversarial scenarios, not just the happy path: confirmed plain
   `/Home` returns `302` to the real current vehicle; confirmed `?showGarage=true` returns `200` and
   bypasses the redirect entirely; set `CurrentVehicleId` to this household's *other* real vehicle
   (id=2, the Volvo) and confirmed `/Home` redirects specifically there, not coincidentally always to
   id=1 - proving the stored preference is actually read, not ignored; set `CurrentVehicleId` to a
   nonexistent id (999) and confirmed graceful fallback to the first accessible vehicle with no error;
   confirmed the new "Vehicles" nav item renders; re-confirmed `/Home/Garage` and `/css/site.css`
   (brace balance) both unaffected. Reset the dev instance's `CurrentVehicleId` back to a sane value
   (1) afterward so the test scenarios didn't leave dev state in a confusing spot.

### Result

Structurally complete, verified against multiple real (not just happy-path) scenarios. **Not yet
visually verified** - the user needs to confirm the actual landing experience feels right (does it
make sense that opening the app now goes straight to a vehicle instead of the Garage grid?) before any
further increments. This is a genuine UX shift, not just a markup port like Increments 2-3b - worth a
real look, not a rubber-stamp.

### Amendment: reverted, per direct user feedback

User's answer: "open on the grid, but leave the option to transition to other cars in the corner."
This is a genuine, deliberate product decision (not a bug report) - the Garage grid stays the landing
experience; the mockup's "Dashboard as default landing" idea is explicitly declined for this app. What
the user DID want kept: quick one-click access to jump to a specific vehicle without navigating
through the grid - i.e. the vehicle switcher already built in Increment 3b, just relocated.

Reverted cleanly rather than layering a workaround on top:
- `HomeController.Index()` back to a bare `return View()` - the `showGarage` parameter and its whole
  resolve-and-redirect branch removed entirely, not just disabled, since nothing needs it anymore.
- `returnToGarage()` back to plain `/Home` - the `showGarage=true` flag it was passing existed only to
  bypass a redirect that no longer exists.
- Added the vehicle switcher (unchanged infrastructure from Increment 3b - same
  `GetVehicleSwitcherList` endpoint, same `_VehicleSwitcherList.cshtml` partial, same
  `loadVehicleSwitcher()`/`switchToVehicle()` JS) to `Home/Index`'s sidebar header too, not just
  `Vehicle/Index`'s - labeled "Jump to Vehicle" rather than "Switch Vehicle" here, since there's no
  "current" vehicle context on the Garage page for "switch" to be relative to.
- `Home/Index`'s header restructured into the same two-sibling pattern `Vehicle/Index` already uses
  (wordmark-click area separate from the switcher dropdown), for the same reason: they can't share one
  onclick handler.

`CurrentVehicleId` (Increment 1) and the switcher's set-and-navigate behavior are unaffected - they
were never the part being reverted, only the auto-redirect-on-landing behavior was.

Verified: `dotnet build` (0 errors), `dotnet test` (10/10), curl confirmed `/Home` now returns `200`
directly (no more `302`), the switcher renders in `Home/Index`'s sidebar with real vehicle data, the
switcher endpoint still works, `Vehicle/Index` and `/Home/Garage` both unaffected, `/css/site.css`
still balanced.

**Result**: Landing behavior reverted to the Garage grid, with a "Jump to Vehicle" quick-switcher now
available from both `Home/Index` and `Vehicle/Index`. This closes out the increment - no further
review needed on the redirect question specifically, since the user's answer settled it directly
rather than needing another round of "does this look right."

## Increment 5: Dashboard hero rebuild

### Task packet

```
TASK ID: PHASE-16-05
TITLE: Rebuild the per-vehicle Dashboard hero band, using only already-fetched data
OBJECTIVE: Bring the existing photo hero + headline stat row closer to the mockup's cohesive "hero
  card" feel, without inventing any new data fields - explicitly in scope per the user's "real data
  only" decision for this phase.
INPUTS: Views/Vehicle/Report/_Report.cshtml (the hero band + stat row markup), Models/Report/
  ReportViewModel.cs and ReportHeader.cs (confirmed exact fields available: MaxOdometer,
  DistanceTraveled, TotalCost, AverageMPG, VehicleImageLocation, VehicleIsSold, VehicleSoldDate - no
  Make/Model/Year/LicensePlate reach this partial), wwwroot/css/site.css's existing .report-hero*/
  .report-stat-* rules and .ct-empty-state's established muted-icon visual language (reused as the
  precedent for the new placeholder, not invented fresh).
ALLOWED SCOPE: Razor/CSS changes only to _Report.cshtml's hero block and new supporting CSS classes -
  no controller/model changes (confirmed unnecessary before starting, not just assumed).
NON-SCOPE: Adding a vehicle title/name into the hero content area (deliberately not done - reused an
  existing, already-articulated design decision from this same codebase: a photo-band comment already
  states "that identity already lives in the nav bar's .lubelogger-vehicle-title just above it, and
  repeating it here would be redundant, not confident" - the sidebar (Increment 3) already shows
  Year/Make/Model, so duplicating it in the Dashboard content would contradict the app's own stated
  design principle, not follow it); any structured Fuel Type/Transmission/Drivetrain/Power/Torque
  spec panel (flagged in the original phase research as requiring new Vehicle fields that don't exist
  - explicitly out of scope this phase); changes to the charts/reminders/collaborator grid below the
  hero (untouched, not part of "hero").
IMPLEMENTATION REQUIREMENTS:
  - A real, easy-to-miss gap found and fixed, not just a restyle: the hero band used to be skipped
    entirely (`@if (Model.VehicleImageLocation != "/defaults/noimage.png")`) for any vehicle without a
    real uploaded photo - and since the SOLD band was nested INSIDE that same conditional, a sold
    vehicle with no photo showed no sold indicator anywhere on its Dashboard at all. Fixed by always
    rendering `.report-hero`, with the photo/placeholder as a conditional CHILD rather than gating the
    whole band, and the sold band as an independent sibling condition.
  - New `.report-hero-placeholder`: reuses `.ct-empty-state`'s established muted-icon visual language
    (large icon, ~35-40% opacity, secondary color) rather than inventing a new empty-state treatment,
    filled with `bi-car-front` (the same icon already used for vehicle iconography elsewhere in this
    app, e.g. the new "Vehicles" sidebar item from Increment 4).
  - New `.report-hero-stats`: replaces the bare `<hr />` between the hero and the stat row with a
    border-top directly on the stat row, so it reads as one continuous hero unit instead of a photo
    followed by an unrelated section - the stats themselves (`.report-stat-value`/`-label`) were
    already using the right typographic treatment (Fraunces hero-axis, tabular-nums, uppercase tracked
    labels - already on this design system's "closed list" for that treatment) and needed no changes.
DELIVERABLES: A hero band with guaranteed presence on every vehicle (photo or placeholder), a working
  sold indicator regardless of photo presence, and a more cohesive hero-to-stats visual transition.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - A real vehicle WITH a photo (this household's actual BMW Z4 and Volvo S80) still renders
    `.report-hero-photo` exactly as before - no regression on the common case.
  - A vehicle WITHOUT a photo renders `.report-hero-placeholder` with the car icon, not an empty gap -
    genuinely exercised, not just read as correct from the code.
  - The sold band's markup/CSS is unchanged from its previously-working form, just repositioned - low
    regression risk even though the exact "sold + no photo" combination couldn't be exercised against
    real data (see below).
  - /css/site.css still parses with balanced braces after the new rules.
  - No regression on the full Vehicle/Index page load or the report partial for real vehicle data.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (real vehicle, has photo)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=2   (real vehicle, has photo)
  curl -X POST http://localhost:5300/api/vehicles/add ... (throwaway vehicle, no photo, for the
    placeholder path specifically - not reachable via the two real vehicles, both of which have
    photos)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId={throwaway}
  curl -X DELETE http://localhost:5300/api/vehicles/delete?id={throwaway}   (cleanup, always)
  curl http://localhost:5300/css/site.css
STOP CONDITION: The photo and placeholder paths both empirically verified (not just the common case);
  the throwaway test vehicle deleted before considering this done, leaving no residue in real data.
```

### What was done

1. Read `Models/Report/ReportViewModel.cs`/`ReportHeader.cs` before assuming what data would be
   available for a "hero rebuild" - confirmed Make/Model/Year never reach this partial at all (only
   odometer/distance/cost/MPG plus the photo/sold fields), which settled the "should the vehicle name
   go in the hero" question by finding this codebase had already answered it: an existing CSS comment
   on the same photo-band explicitly reasons that repeating the identity here (already shown in the
   Increment 3 sidebar) "would be redundant, not confident." Followed that existing precedent rather
   than re-deciding it.
2. Found the photo-gating bug while reading the current markup to plan the change, not by going
   looking for bugs specifically: the entire hero band, sold indicator included, silently disappeared
   for any vehicle without an uploaded photo. Fixed by restructuring the conditional so the hero band
   always renders, with photo-vs-placeholder as an inner choice and the sold band as an independent
   sibling.
3. Built `.report-hero-placeholder` reusing `.ct-empty-state`'s already-established muted-icon
   language (same opacity/color approach) rather than designing a new empty-state treatment from
   scratch, and `.report-hero-stats` to replace a bare `<hr>` with a border-top that reads as part of
   the hero rather than a new section.
4. Verified the common case first: both of this household's real vehicles (BMW Z4, Volvo S80) still
   render `.report-hero-photo` exactly as before, no regression.
5. To verify the new placeholder path for real (not just read the code and assume), created a
   throwaway vehicle via `/api/vehicles/add` (no photo) - confirmed `.report-hero-placeholder` and the
   `bi-car-front` icon render correctly. Attempted to also verify the sold-band-without-photo
   combination by marking that same throwaway vehicle sold via `/api/vehicles/update`, but discovered
   along the way that `SoldDate` isn't one of the fields this particular API endpoint updates at all
   (grepped the whole controller method for `SoldDate` - zero matches - it's an MVC-only field, same
   category as other fields this codebase's own STATE.md already flags as API-DTO gaps). Rather than
   spend more effort routing around an unrelated pre-existing API limitation, accepted this one
   specific combination as verified by construction (the sold-band code itself is unchanged, working,
   pre-existing markup - only its position moved) - consistent with this exact codebase's own prior
   precedent in `UI_TRANSITION.md` for states that are hard to reach without mutating real data.
   Deleted the throwaway vehicle immediately after (`/api/vehicles/delete?id=3`), confirmed via
   `/api/vehicles` that only the two real vehicles remain.
6. Final regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), full `Vehicle/Index` page
   load unaffected, `/css/site.css` still balanced (427/427, +3 from the two new rule blocks).

### Result

Complete and verified for the two reachable paths (real photo, no photo) against both real data and a
throwaway test case, cleaned up afterward. The third path (sold + no photo) is verified by construction
only, consistent with established precedent in this codebase for states real data can't reach without
mutation. **Not yet visually verified** - same standing caveat as every increment in this phase.

User looked at the live result and asked a fair question - "where are the differences I should be
seeing?" - since Increment 5's fix doesn't apply to either of this household's real vehicles (both have
photos). Answered directly: most of what's visible is Increments 1-4 (the sidebar); the mockup-matching
visual transformation is still ahead in Increments 6-11. Confirmed via AskUserQuestion to continue.

## Increment 6: Quick Actions tile grid

### Task packet

```
TASK ID: PHASE-16-06
TITLE: Add a 2x2 Quick Actions tile grid to the Dashboard, wired to real existing add-record modals
OBJECTIVE: Give the Dashboard one-click access to the most common "add a record" actions, matching
  the mockup's Quick Actions tiles - reusing existing modal-opening JS, not building new forms.
INPUTS: wwwroot/js/{gasrecord,servicerecord,planrecord,vehicle}.js (the four showAddXModal() functions
  - all confirmed callable with zero arguments before wiring anything up, not assumed), Views/Vehicle/
  Index.cshtml's existing modal shells (confirmed reminderRecordModal lives there directly; gas/
  service/plan modal shells do NOT - see the real bug found below).
ALLOWED SCOPE: New markup in _Report.cshtml, new CSS classes, a small optional-callback-parameter
  addition to 4 existing tab-loader functions in vehicle.js (getVehicleGasRecords/
  ServiceRecords/Reminders/PlanRecords) - backward compatible, no existing call site needed to change.
NON-SCOPE: Any new modal/form (all four reused as-is); a "Log a Trip" tile (Trips don't exist -
  explicitly out of scope this phase, per the user's "real data only" decision).
IMPLEMENTATION REQUIREMENTS:
  - A real bug found and fixed BEFORE it shipped, not discovered by luck: each record type's "add"
    modal shell (e.g. #gasRecordModal) is defined inside that record type's OWN tab partial (_Gas.cshtml,
    _ServiceRecords.cshtml, _PlanRecords.cshtml - confirmed by grep, not assumed), not in Vehicle/
    Index.cshtml itself. Tab panes lazy-load on first activation (wwwroot/js/vehicle.js's show.bs.tab
    handler) and get CLEARED when you navigate away "to help with performance" - so on a fresh page
    load landing on the Dashboard (the default tab), the Gas/Service/Plan tabs have never been visited
    and their modal shells don't exist in the DOM at all yet. A Quick Action tile calling
    showAddGasRecordModal() directly from the Dashboard would have silently done nothing (AJAX fires,
    $("#gasRecordModalContent").html(data) targets an empty jQuery selection, modal never shows) for 3
    of the 4 tiles - the reminder one alone would have worked, since its shell lives directly in
    Vehicle/Index.cshtml, not per-tab.
  - Fixed by adding an optional `onLoaded` callback parameter to the four getVehicleX() loader
    functions (invoked after the existing `.html(data)` injection succeeds), rather than duplicating
    each modal's fetch-and-show logic in a new set of wrapper functions, or switching the visible tab
    (which would work but changes what the user sees more than necessary - the modal overlays
    correctly regardless of which tab is nominally active underneath). Each Quick Action tile's onclick
    is `getVehicleX(GetVehicleId().vehicleId, showAddXModal)` - fetch that tab's content (populating
    its modal shell) first, then open the modal, both reusing existing, already-correct functions.
  - Icons reused from elsewhere in this app's own iconography (bi-fuel-pump/bi-card-checklist/
    bi-bell/bi-bar-chart-steps already used for these exact record types in the sidebar), not invented.
DELIVERABLES: Four working Quick Action tiles, each verified to actually open its modal - not just
  that the markup renders.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - All four tiles render with correct onclick handlers referencing the real loader/modal function
    pairs.
  - Each target tab's AJAX response (GetGasRecordsByVehicleId etc.) confirmed to actually contain its
    modal shell - the premise the whole fix depends on, verified directly rather than assumed from
    reading the .cshtml files alone.
  - Each add-record partial (GetAddGasRecordPartialView etc.) confirmed to return real, error-free
    content.
  - No regression on the full Vehicle/Index page for either real vehicle, or on /css/site.css's brace
    balance.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1 | grep -o 'onclick="getVehicle[A-Za-z]*(GetVehicleId'
  curl http://localhost:5300/Vehicle/GetGasRecordsByVehicleId?vehicleId=1 | grep -o 'id="gasRecordModal"'
  curl http://localhost:5300/Vehicle/GetAddGasRecordPartialView?vehicleId=1   (repeat pattern for service/plan/reminder)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/css/site.css
STOP CONDITION: The lazy-load premise verified empirically for all 4 tiles (endpoint responses
  actually contain the modal shell / real form content), not inferred from reading the partials alone -
  this is exactly the kind of bug that reads as "obviously fine" from the Razor markup and only shows
  up when you trace the actual runtime DOM lifecycle.
```

### What was done

1. Confirmed all four `showAddXModal()` functions are callable with zero arguments before wiring
   anything to them - read each one rather than assuming from its name.
2. While confirming where each modal's SHELL lives (not just its content-providing endpoint), found
   the real bug: `#gasRecordModal`/`#serviceRecordModal`/`#planRecordModal` are defined inside their
   own tab's lazily-loaded partial, not in `Vehicle/Index.cshtml`. Combined with `vehicle.js`'s
   existing "clear the tab you're leaving, for performance" behavior, this meant 3 of the 4 planned
   Quick Action tiles would have silently done nothing on a fresh page load (the Dashboard tab is the
   default, so Gas/Service/Plan would never have been visited yet that session) - only the Reminder
   tile, whose modal shell happens to live directly in `Vehicle/Index.cshtml`, would have worked.
3. Fixed by adding an optional `onLoaded` callback to the four `getVehicleX()` loader functions in
   `vehicle.js` - each Quick Action tile now fetches its target tab's content first (which populates
   the modal shell as a side effect, being the same partial that also contains the data grid), then
   opens the add-modal in the callback. Chose this over switching the visible tab (works, but changes
   more of what the user sees than necessary) or duplicating each modal's fetch logic into new
   functions (would diverge from the already-correct existing loaders over time).
4. Built the 2x2 tile grid in `_Report.cshtml` (Add Fuel/Add Service/Add Reminder/Add Planned Work),
   reusing icons already used for these exact record types elsewhere in this app (sidebar nav), and
   `.report-quick-action` CSS matching the existing flat/bordered tile language.
5. Verified the fix's actual premise empirically, not just re-read the code and trusted it: curled
   `GetGasRecordsByVehicleId`/`GetServiceRecordsByVehicleId`/`GetPlanRecordsByVehicleId` and confirmed
   each response really does contain its modal shell id; curled each `GetAddXRecordPartialView`
   endpoint and confirmed real, error-free form content comes back; confirmed the reminder tab's
   response does NOT contain a `reminderRecordModal` id (its shell truly is only in `Vehicle/
   Index.cshtml`, exactly as suspected) - a harmless one-extra-fetch for that specific tile, not a bug.
6. Regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), both real vehicles' full
   `Vehicle/Index` pages still load, `/css/site.css` still balanced (431/431).

### Result

Complete, and the one genuinely risky part (would the modals actually open) verified against real
endpoint responses rather than assumed from the markup. **Not yet visually verified** - same standing
caveat as every increment in this phase; specifically, whether *clicking* a tile in a live browser
actually opens the modal (not just that the underlying AJAX chain is provably correct) hasn't been
exercised by this agent.

## Increment 7: Fuel Economy widget (real sparkline)

### Task packet

```
TASK ID: PHASE-16-07
TITLE: Add a Fuel Economy stat card with a real Chart.js sparkline, first of the mockup's widget row
OBJECTIVE: Match the mockup's "number + trend line" card pattern using only already-computed server
  data (FuelMileageForVehicleByMonth, already built for the existing full-size MPG-by-month chart) -
  a genuine sparkline, not a fake CSS trend indicator, since the exact monthly series already exists.
INPUTS: Views/Vehicle/Report/_MPGByMonthReport.cshtml (studied as the existing, working Chart.js
  pattern for this exact data - reused its empty-state condition and chart-color helpers rather than
  inventing new ones), Models/Report/MPGForVehicleByMonth.cs's CostData (confusingly named - holds
  average MPG per month, not a cost, in this specific model), ReportHeader.AverageMPG (the
  already-formatted headline string).
ALLOWED SCOPE: One new partial (Report/_FuelEconomyWidget.cshtml), a new widget-row container in
  _Report.cshtml (left open for Increments 8-10 to append their own cards into), new
  .report-widget-* CSS - no controller/model changes, no changes to the existing full-size MPG chart.
NON-SCOPE: Removing or replacing the existing full-size MPG-by-month chart lower on the page (kept -
  it offers real interactivity, a year filter and metrics toggle, that a glance-level sparkline
  doesn't attempt to replace); any other widget-row card (Increments 8-10).
IMPLEMENTATION REQUIREMENTS:
  - Sparkline: type 'line', x/y scales both display:false, no legend/title/tooltip, thin (2px) line,
    no points, borderColor via the existing getChartTextColor() helper (already reads live CSS custom
    properties so it themes correctly light/dark - reused, not reinvented).
  - Empty state: identical condition to the existing full chart's own gate
    (`mpgData.CostData.Any(x => x.Cost > 0)`) - found while checking why the currently-visible Dashboard
    showed "0 mpg" as its headline stat that NEITHER real vehicle in this household has enough gas
    history to compute a monthly MPG series yet, so the empty state is the actually-exercised path for
    real data right now, not a hypothetical edge case that can be skipped.
  - Widget row deliberately left open-ended (`<div class="row ... gy-2"><div class="col-md-3
    col-12">...Fuel Economy...</div></div>`) with a comment noting Increments 8-10 append their cards
    into the same row, rather than hardcoding a 1-column layout that would need restructuring later.
DELIVERABLES: A working Fuel Economy card, both its empty and populated states verified against real
  chart-rendering output, not just read as correct from the Razor.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - Both real vehicles (neither has enough gas history for MPG yet) render the "No Data" empty state
    correctly - the actually-common case for this household right now.
  - A vehicle WITH enough gas history (2 fill-ups, a real delta-mileage/consumption pair) renders the
    populated branch: sparkline canvas present, Chart.js call present, headline number matches the
    real computed MPG value - not just "doesn't error," the actual number was checked against the
    known input (400 miles / 40 gallons = 10.00 mpg, matching the test data exactly).
  - No regression on either vehicle's full Vehicle/Index page or /css/site.css's brace balance.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (real vehicle, empty state)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=2   (real vehicle, empty state)
  curl -X POST http://localhost:5300/api/vehicles/add ...                (throwaway vehicle)
  curl -X POST http://localhost:5300/api/vehicle/gasrecords/add ... (x2, a month apart, known
    mileage delta and fuel consumed, to produce a verifiable MPG value)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId={throwaway}   (populated branch)
  curl -X DELETE http://localhost:5300/api/vehicles/delete?id={throwaway}        (cleanup, always)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/css/site.css
STOP CONDITION: Both the empty state (against real data) and the populated state (against a
  known-value throwaway test) verified - not just one or the other - before the throwaway vehicle is
  deleted and this increment is considered done.
```

### What was done

1. Read `_MPGByMonthReport.cshtml` (the existing full-size chart for the same underlying data) before
   designing anything new - reused its exact empty-state condition, its `getChartTextColor()` helper
   for theme-correct coloring, and confirmed `CostData`'s `Cost` field actually holds average MPG in
   this specific model (a naming quirk inherited from a shared `CostForVehicleByMonth` type reused
   across cost and MPG reporting) rather than guessing at the data shape.
2. While checking why the live Dashboard's headline stat showed "0 mpg" (visible in the screenshot the
   user shared before this increment started), confirmed neither real vehicle in this household has
   enough gas record history yet for MPG to compute for any month - meaning the empty state isn't a
   hypothetical edge case for this increment, it's the actually-exercised path against real data right
   now. Built it as a first-class state, not an afterthought.
3. Built `Report/_FuelEconomyWidget.cshtml`: headline from the already-formatted
   `ReportHeaderForVehicle.AverageMPG`, a real Chart.js line sparkline (hidden axes/legend/tooltip,
   thin line, no points) fed directly by `FuelMileageForVehicleByMonth.CostData` - the exact same data
   source as the existing full chart, no new query.
4. Added an open-ended widget-row container in `_Report.cshtml` (one populated column so far, a
   comment noting Increments 8-10 append their own cards into the same row) rather than a fixed
   single-column layout that would need restructuring when the next widgets arrive.
5. Verified BOTH states against real behavior, not just one: confirmed both real vehicles correctly
   show "No Data" (the empty state, actually exercised); created a throwaway vehicle, added two gas
   records a month apart with a known mileage delta (400 mi) and fuel consumed (40 gal), confirmed the
   populated branch renders the sparkline canvas + Chart.js call AND that the headline number is
   exactly correct (10.00 mpg, matching 400/40 precisely, not just "a number appeared") - then deleted
   the throwaway vehicle and confirmed via `/api/vehicles` only the two real vehicles remain.
6. Regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), both real vehicles' pages still
   load, `/css/site.css` still balanced (436/436).

### Result

Complete, with both the empty and populated states genuinely exercised against real rendering output -
not just the common case, and not just assumed correct from reading the Razor/JS. **Not yet visually
verified** - same standing caveat as every increment in this phase.

## Increment 8: Total Spent widget

### Task packet

```
TASK ID: PHASE-16-08
TITLE: Add a Total Spent stat card to the Dashboard widget row, second of the mockup's row
OBJECTIVE: Match the mockup's "number + bar trend" card pattern for total cost, reusing the exact
  data source already computed for the existing full-size expenses chart - no new query.
INPUTS: Views/Vehicle/Report/_GasCostByMonthReport.cshtml (the existing chart for this exact data -
  studied for its empty-state condition and data shape before building anything new), ReportViewModel.
  CostForVehicleByMonth (the monthly series) and ReportHeaderForVehicle.TotalCost (the headline value,
  already computed), Helper/StaticHelper.cs's HideZeroCost (the existing currency-formatting/zero-
  hiding helper already used by _ReportHeader.cshtml's own Total Cost stat - reused for the exact same
  formatting convention rather than a fresh ToString("C2") call).
ALLOWED SCOPE: One new partial (Report/_TotalSpentWidget.cshtml), one new column appended to
  Increment 7's already-open-ended widget row - no new CSS needed (reuses .report-widget-* as-is).
NON-SCOPE: Removing/replacing the existing full-size expenses-by-month chart lower on the page (kept,
  same reasoning as Increment 7 - it has real interactivity this summary card doesn't attempt).
IMPLEMENTATION REQUIREMENTS:
  - Sparkline: type 'bar' this time (distinguishing it visually from Fuel Economy's line sparkline,
    matching the mockup's own bar-vs-line distinction between these two specific cards), same hidden-
    axes/no-legend/no-tooltip treatment as Increment 7's sparkline.
  - Empty state: reused `_GasCostByMonthReport.cshtml`'s own condition
    (`costData.Any(x => x.Cost > 0)`, dropped the `|| DistanceTraveled > 0` half since this card is
    cost-only, not the combined cost+distance chart) rather than inventing a new one.
  - Headline formatting: `StaticHelper.HideZeroCost(Model.ReportHeaderForVehicle.TotalCost.ToString("C2"), true)`
    - the exact same call `_ReportHeader.cshtml`'s own "Total Cost" stat already uses, for currency-
    symbol/zero-hiding consistency across the page rather than a fresh formatting decision.
DELIVERABLES: A working Total Spent card, both states verified against real vehicle data (no
  throwaway vehicle needed this time - vehicle 1 already has real cost history).
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - Vehicle 1 (has known real cost history - a GBP260.00 total, visible in the screenshot the user
    shared earlier this phase) renders the populated branch with the headline reading exactly
    "£260.00" - matched against the known real value, not just "some currency string appeared."
  - Vehicle 2 (no cost history) renders the "No Data" empty state correctly.
  - No regression on either vehicle's full page or /css/site.css's brace balance (unchanged from
    Increment 7 - no new CSS rules needed, confirming the reuse claim rather than assuming it).
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (populated - real data)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=2   (empty - real data)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/css/site.css
STOP CONDITION: Both states verified against real vehicle data specifically (this increment got
  lucky - unlike Increment 7, no throwaway vehicle was needed since vehicle 1 already has real cost
  history) - the headline number checked against the exact known value, not just presence.
```

### What was done

1. Read `_GasCostByMonthReport.cshtml` (the existing chart for the same underlying `CostForVehicleByMonth`
   data) before building anything new - reused its empty-state condition (dropping the
   `DistanceTraveled` half, irrelevant to a cost-only card) and confirmed the data shape rather than
   guessing.
2. Built `Report/_TotalSpentWidget.cshtml`: headline via `StaticHelper.HideZeroCost(...)` - the exact
   same formatting call `_ReportHeader.cshtml`'s own Total Cost stat already uses, for consistency -
   and a bar-type sparkline (deliberately distinct from Fuel Economy's line type, matching the
   mockup's own visual distinction between these two cards) fed by `CostForVehicleByMonth` directly.
3. Appended it as a second column into Increment 7's already-open-ended widget row - no new CSS
   needed, confirmed rather than assumed (checked the CSS brace count was unchanged after the build).
4. Verified against real data directly - no throwaway vehicle needed this round, since vehicle 1
   already has real cost history from earlier phase testing: confirmed the populated branch renders
   with the headline reading exactly "£260.00," matching the exact value already visible in the
   screenshot the user shared before this increment started (not just "a currency string appeared").
   Confirmed vehicle 2 (no cost history) correctly renders the empty state.
5. Regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), both vehicles' pages still load,
   `/css/site.css` still balanced at the same count as Increment 7 (436/436, confirming no new CSS was
   actually needed rather than just assuming the reuse claim).

### Result

Complete, both states verified against real vehicle data with an exact-value match on the populated
case. **Not yet visually verified** - same standing caveat as every increment in this phase.

## Increment 9: Planned Maintenance widget

### Task packet

```
TASK ID: PHASE-16-09
TITLE: Add a Planned Maintenance list to the Dashboard widget row, reusing IReminderHelper's
  already-computed urgency output
OBJECTIVE: Surface upcoming/overdue reminders directly on the Dashboard, sorted by urgency then due
  date - the first widget-row card that's a list rather than a stat+sparkline.
INPUTS: Helper/ReminderHelper.cs's GetReminderRecordViewModels (already computes Urgency/DueDays/
  DueMileage per reminder - confirmed the exact shape by reading the method, not assumed),
  Controllers/Vehicle/ReportController.cs's existing GetReportPartialView action (already calls
  GetRemindersAndUrgency and computes the full reminder list in memory, but previously only kept the
  aggregate COUNTS for the pie chart - the per-reminder list itself was computed then discarded).
ALLOWED SCOPE: A new ReportViewModel.UpcomingReminders field + one line in the controller assigning it
  from the already-computed `reminders` variable (sorted/truncated) - a small, justified model/
  controller change, not a pure Razor/CSS increment like 7-8, but the underlying computation itself is
  not new. One new partial (Report/_PlannedMaintenanceWidget.cshtml), new .report-widget-list* CSS.
NON-SCOPE: Any change to the existing reminder urgency pie chart (_ReminderMakeUpReport.cshtml,
  untouched) or the full Reminders tab.
IMPLEMENTATION REQUIREMENTS:
  - Controller: `viewModel.UpcomingReminders = reminders.OrderByDescending(x => x.Urgency)
    .ThenBy(x => x.DueDays).Take(5).ToList();` - sorting/truncation done once in the controller, not
    the view, keeping the partial itself dumb (just renders a pre-sorted list), consistent with how
    every other ReportViewModel field is already fully-prepared before reaching the view.
  - Badge classes: first real adoption of `.status-badge-spotlight` for its originally-documented
    purpose - the CSS comment introducing it literally says "e.g. an overdue reminder on a Garage
    card," written when the primitive was built but never actually wired to a real caller until now.
    PastDue -> spotlight, VeryUrgent -> danger, Urgent -> warning, NotUrgent -> neutral.
  - Badge label text: `reminder.Urgency.ToString()` (raw enum name) was rejected in favor of matching
    this app's own established translation-key convention for these exact urgency labels ("Past Due",
    "Very Urgent", "Urgent", "Not Urgent" - found already in use in
    Views/Vehicle/Reminder/_ReminderRecords.cshtml and Views/Kiosk/_Kiosk.cshtml) - a small but real
    correctness fix over the naive enum-to-string approach, caught before it shipped.
  - Due text: `reminder.Metric == ReminderMetric.Date ? reminder.Date.ToShortDateString() :
    reminder.Mileage` - matches the exact existing convention already used in the full Reminders
    table (_ReminderRecords.cshtml), not invented fresh.
  - List variant reuses the same outer `.report-widget-card` wrapper as Increments 7-8's stat cards
    (confirmed its flex-column layout accommodates a list of children just as well as a single big
    stat value - no need for a structurally different card shape, despite flagging this as worth
    checking in STATE.md before starting).
DELIVERABLES: A working Planned Maintenance list, both empty and populated states verified against
  real rendering, including the badge/urgency mapping specifically.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - Both real vehicles (neither has any reminders yet) render the "No Data" empty state correctly -
    confirmed real, not assumed, and NOT confused with unrelated pre-existing `.status-badge-*` usage
    already present elsewhere on the same page (Government Data panel - see below).
  - A throwaway past-due reminder added to a real vehicle renders in the list with the correct
    "Past Due" label and `.status-badge-spotlight` class - the actual urgency computation and badge
    mapping verified end-to-end, not just "some list item appeared."
  - No regression on either vehicle's full page or /css/site.css's brace balance.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (empty state initially)
  curl -X POST http://localhost:5300/api/vehicle/reminders/add?vehicleId=1 ... (throwaway, past-due)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (populated, verify badge)
  curl -X DELETE http://localhost:5300/api/vehicle/reminders/delete?id={throwaway}   (cleanup, always)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/css/site.css
STOP CONDITION: The throwaway reminder's exact urgency/badge mapping verified (not just "a row
  appeared"), then deleted and the empty state reconfirmed, before this increment is considered done.
```

### What was done

1. Read `ReminderHelper.GetReminderRecordViewModels` and `ReportController.GetReportPartialView`
   before assuming a data source existed - found the full per-reminder list (with Urgency/DueDays/
   DueMileage already computed) was already being calculated in the controller, just discarded after
   extracting only the aggregate counts for the pie chart. Added
   `ReportViewModel.UpcomingReminders` and one controller line to keep the already-computed list
   instead of throwing it away - not a new computation, just no longer discarding an existing one.
2. Built `Report/_PlannedMaintenanceWidget.cshtml` as a list variant of the `.report-widget-card`
   pattern - confirmed the existing flex-column card layout accommodates a list of rows just as well
   as a single stat value, no structurally different card needed.
3. While writing the badge label text, caught a real small mistake before it shipped: translating
   `reminder.Urgency.ToString()` (the raw enum name, e.g. "PastDue" with no space) would have produced
   an ugly, inconsistent label. Checked this app's own existing conventions first
   (`_ReminderRecords.cshtml`, `_Kiosk.cshtml`) and matched their exact established translation-key
   strings ("Past Due", "Very Urgent", etc.) instead.
4. Wired the badge CSS classes to `.status-badge-spotlight`/`-danger`/`-warning`/`-neutral` - the first
   real adoption of `.status-badge-spotlight` for the exact use case its own introducing comment named
   ("an overdue reminder on a Garage card") but had never actually been wired up for.
5. Verified the empty state against both real vehicles (neither has any reminders yet) - and while
   checking for stray `.status-badge` matches in the rendered output to confirm nothing was
   miscounted, found and correctly attributed 4 UNRELATED `.status-badge-*` usages already present on
   the same page from the existing Government Data panel (Phase 8's mocked DVLA/DVSA work) - meaning
   this CSS primitive's own "not yet adopted by any view" comment is actually stale documentation
   predating that phase, not a bug in this increment. Didn't "fix" the stale comment (out of scope,
   not asked, would be scope creep beyond this increment).
6. Verified the populated branch for real: added a throwaway past-due reminder to vehicle 1 via
   `/api/vehicle/reminders/add`, confirmed it rendered with exactly the "Past Due" label and
   `.status-badge-spotlight` class (the actual urgency computation and badge mapping, not just "a row
   appeared") - then deleted it and reconfirmed the empty state was restored.
7. Regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), both vehicles' pages still load,
   `/css/site.css` still balanced (441/441).

### Result

Complete, with a real small correctness catch (translation-key convention over raw enum names) made
before shipping, and both states verified against real rendering including the specific urgency/badge
mapping. **Not yet visually verified** - same standing caveat as every increment in this phase.

## Increment 10: Recent Activity widget

### Task packet

```
TASK ID: PHASE-16-10
TITLE: Add a Recent Activity feed to the Dashboard widget row, the last data widget in this phase
OBJECTIVE: Surface the latest N maintenance/cost events across record types, matching the mockup's
  mixed-type activity feed - without misusing the existing GetVehicleHistory method, which serves a
  different, heavier purpose.
INPUTS: Controllers/Vehicle/ReportController.cs's GetVehicleHistory (read in full before assuming it
  could be reused directly - confirmed it's a full filterable history REPORT generator with tag/date-
  range filtering and depreciation calculations, returning VehicleHistoryViewModel, a shape carrying
  purchase-price/depreciation fields entirely irrelevant to a simple feed - calling it from a Dashboard
  widget would have been semantically wrong, not just inefficient), its GenericReportModel projection
  pattern for Service/Repair/Upgrade/Tax records (reused for the mapping shape, not the method itself),
  and GetReportPartialView's own already-fetched serviceRecords/collisionRecords/upgradeRecords/
  taxRecords/gasRecords locals (all in scope already - zero new queries needed).
ALLOWED SCOPE: A new ReportViewModel.RecentActivity field + inline projection logic added directly to
  the existing GetReportPartialView action (not a new controller method, since all the source data
  was already local to that action) - reuses GenericReportModel's existing shape and adds GasRecord,
  which GetVehicleHistory's own projection confirmed omits. One new partial
  (Report/_RecentActivityWidget.cshtml), new .report-widget-list-icon CSS.
NON-SCOPE: Any change to GetVehicleHistory or the "Vehicle Maintenance Report" export feature it
  serves (untouched, different purpose, different audience).
IMPLEMENTATION REQUIREMENTS:
  - `_translationHelper` is not injected in ReportController (found by trying to use it and checking,
    not assumed) - moved the "Fuel" label decision for gas records (which have no Description field of
    their own) into the VIEW instead of the controller, where `ITranslationHelper` is already available
    via the standard partial-injection pattern every other widget partial already uses. The controller
    leaves GasRecord's GenericReportModel.Description at its default empty string; the partial fills
    in "Fuel" specifically when DataType is GasRecord and Description is blank.
  - Icon-per-record-type mapping reuses the exact same Bootstrap Icon names already used for these
    record types in the sidebar nav (Increment 3) - bi-card-checklist/bi-exclamation-octagon/
    bi-wrench-adjustable/bi-currency-dollar/bi-fuel-pump - visual consistency with the nav rather than
    a fresh icon choice per widget.
  - Sort: `recentActivity.OrderByDescending(x => x.Date).Take(6)` - newest first, capped at 6.
DELIVERABLES: A working Recent Activity feed, verified against real vehicle data with an independent
  cross-check against Increment 8's Total Spent figure.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - Vehicle 1 (known real history) renders 3 real activity items, correctly sorted newest-first, with
    correct icons per type and correct cost formatting.
  - The three displayed costs sum to exactly the same £260.00 already shown by Increment 8's Total
    Spent widget - an independent consistency check across two different widgets pulling from
    overlapping data, not just "some rows appeared."
  - Vehicle 2 (no history) renders the "No Data" empty state.
  - No regression on either vehicle's full page or /css/site.css's brace balance.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1   (populated - real data)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=2   (empty - real data)
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/css/site.css
STOP CONDITION: The populated case's actual content (descriptions, dates, costs, sort order) checked
  against known real records, including the cross-widget cost-sum consistency check, not just "some
  content rendered."
```

### What was done

1. Read `GetVehicleHistory` in full before assuming it could be reused - confirmed it's a heavyweight,
   differently-purposed method (tag/date-range filtering, depreciation calculations, feeds the
   "Vehicle Maintenance Report" export) returning a shape with fields irrelevant to a simple feed.
   Reused only its `GenericReportModel` projection PATTERN, not the method itself, avoiding both
   duplicated business logic and a semantically wrong dependency.
2. Added the projection directly into the already-existing `GetReportPartialView` action rather than
   a new controller method, since every source list (`serviceRecords`/`collisionRecords`/
   `upgradeRecords`/`taxRecords`/`gasRecords`) was already fetched and in scope there - genuinely zero
   new queries, not just "reuses a helper."
3. Hit a real small issue immediately: tried using `_translationHelper` in the controller for the
   gas-record "Fuel" label and found it isn't injected there at all. Rather than adding a new
   controller dependency for one label, moved that decision into the widget partial itself, where
   `ITranslationHelper` is already available via the same injection pattern every other widget partial
   in this phase already uses - a cleaner fix than the alternative.
4. Built `Report/_RecentActivityWidget.cshtml` with an icon-per-record-type mapping reusing the exact
   same Bootstrap Icon names already used for these record types in the Increment 3 sidebar, for
   visual consistency rather than a fresh per-widget icon choice.
5. Verified against real data with an unusually strong cross-check: vehicle 1 rendered 3 real activity
   items ("Initial service after purchase" £40, "Radio replaced" £20, "ECU Repaired for CHECKSUM RAM
   Failure" £200), correctly sorted newest-first, with correct icons - and the three costs sum to
   exactly £260.00, matching Increment 8's Total Spent figure precisely. This is an independent
   consistency check across two different widgets built in two different increments, both pulling
   from overlapping real data, not just "some rows appeared." Vehicle 2 (no history) correctly showed
   the empty state.
6. Regression pass: `dotnet build` (0 errors), `dotnet test` (10/10), both vehicles' pages still load,
   `/css/site.css` still balanced (442/442).

### Result

Complete. The last data widget in this phase, verified with a real cross-widget consistency check
(matching Increment 8's total exactly) rather than just confirming content appeared. **Not yet visually
verified** - same standing caveat as every increment in this phase. Increment 11 (Magneto-motif promo
tile) is the last increment - purely cosmetic, no data.

## Increment 11: Magneto-motif promo tile (last increment)

### Task packet

```
TASK ID: PHASE-16-11
TITLE: Add a promo tile to the Dashboard using the existing chapter-divider ink-band motif
OBJECTIVE: Close out the mockup's layout with its magazine-ad-style banner, using this app's own
  already-established editorial visual language - honestly, not by inventing fake magazine content.
INPUTS: wwwroot/css/site.css's .report-hero-sold-band (the existing "chapter-divider" ink-band pattern
  - full-bleed solid colour, bold Fraunces hero-axis typography - studied as the pattern to extend,
  not duplicate), the Documents sidebar tab (id="documents-tab", already wired via Increment 3).
ALLOWED SCOPE: New markup in _Report.cshtml (a single promo tile, positioned after the widget row),
  new .report-promo-tile* CSS reusing the sold-band's existing color/typography tokens.
NON-SCOPE: Any new data, any fabricated "article" content, any change to the Documents tab itself.
IMPLEMENTATION REQUIREMENTS:
  - A real content decision, not a technical one: the mockup's actual promo tile advertised a specific
    (real, from an actual magazine) article - "E46 M3 - A modern classic." This app has no equivalent
    real content to promote, and inventing a fake article/headline would mean shipping fabricated
    content in a real user-facing product - not acceptable. The honest translation is to keep the
    VISUAL motif (the ink-band + bold serif headline this app already uses for its "chapter closed"
    sold-vehicle moment) while pointing it at something real: the vehicle's own Documents tab.
  - Click target: `bootstrap.Tab.getOrCreateInstance(document.getElementById('documents-tab')).show()`
    - the real Bootstrap 5 JS Tab API (this app already loads bootstrap.bundle.min.js, confirmed
    globally available), which triggers the exact same `show.bs.tab` handler that correctly lazy-loads
    the Documents tab's content (the same mechanism Increment 6 had to learn about the hard way) -
    reused correctly this time, not re-discovered as a bug.
  - CSS reuses `.report-hero-sold-band`'s exact color tokens (`--bs-primary` background, `--bs-body-bg`
    text) and Fraunces hero-axis font-variation-settings for the headline, rather than introducing new
    color values for what is visually the same "chapter-divider" family of element.
DELIVERABLES: A working promo tile, the final piece of Phase 16's Dashboard widget work.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors. dotnet test: 10/10 passing.
  - Tile renders on both real vehicles' Dashboard, correctly targeting the real documents-tab id.
  - No regression on either vehicle's full page, Home/Garage, or /css/site.css's brace balance.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5300 --no-build (background)
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=1
  curl http://localhost:5300/Vehicle/GetReportPartialView?vehicleId=2
  curl http://localhost:5300/Vehicle/Index?vehicleId=1
  curl http://localhost:5300/Vehicle/Index?vehicleId=2
  curl http://localhost:5300/Home/Garage
  curl http://localhost:5300/css/site.css
STOP CONDITION: Structural verification green across both vehicles and a full-app regression sweep
  (Home/Garage included, not just the two Vehicle pages this phase has focused on) - this closes out
  the whole phase, so the regression check widened accordingly.
```

### What was done

1. Faced a real content decision before writing any code: the mockup's actual promo tile advertised a
   specific real magazine article. Rather than inventing a fake headline/article to visually match it
   - which would mean shipping fabricated content dressed up as real - kept the motif (this app's
   existing ink-band "chapter-divider" pattern, already used for the sold-vehicle indicator) and
   pointed it at something genuinely real: the vehicle's own Documents tab.
2. Wired the click target using the real Bootstrap 5 `bootstrap.Tab` JS API rather than a bare
   `.click()` simulation, triggering the exact `show.bs.tab` lazy-load handler Increment 6 had to
   learn about the hard way - applied correctly here from the start, not rediscovered as a bug.
3. Built `.report-promo-tile*` CSS reusing `.report-hero-sold-band`'s exact existing color tokens and
   Fraunces hero-axis typography treatment, rather than introducing new values for what is visually
   the same design-system "family" of element.
4. Verified structurally across both real vehicles, plus a full-app regression sweep since this closes
   out the whole phase (not just the two Vehicle Dashboard pages this phase has focused on):
   `dotnet build` (0 errors), `dotnet test` (10/10), both vehicles' Dashboard tiles render correctly
   targeting the real `documents-tab` id, `Home/Garage` unaffected, `/css/site.css` still balanced
   (446/446).

### Result

Complete. This closes Phase 16's planned 11 increments. **The whole phase still needs a real, live
visual review from the user** - every increment from 1 through 11 has been verified structurally
(build, tests, curl-inspected markup/CSS, and where real data existed, cross-checked against known
values) but never actually seen rendered by this agent, since no browser/screenshot tool exists in
this environment. The user has done spot-checks along the way ("better, carry on" / "bang on, continue"
/ the landing-page decision / "where are the differences" question) but a full end-to-end look at the
finished Dashboard, on both desktop and the phone's installed PWA (Phase 15), is the natural next step
before considering this phase truly done.

## Post-completion: production deployment gap found and fixed

When the user actually checked the finished Dashboard on their phone (the way they'd really use this
app day to day, per the entire point of Phase 15), they saw the *old* pre-Phase-16 UI. Root cause: all
11 increments had only ever been built and verified against the **dev instance** (`dotnet run` on port
5300, in the repo checkout) - never actually deployed to the production Windows Service
(`C:\Services\CarTracker`) that the phone reaches via Tailscale. The production binaries hadn't been
rebuilt since Phase 15 (confirmed via file timestamp - last built 2026-08-18 14:29, before Phase 16
started). This was a real process gap, not a code bug: "verified against the dev instance" was silently
treated as equivalent to "the user can see this," which it wasn't. Recorded as a lesson for future
phases - explicitly deploy to production and verify via the real Tailscale URL before telling the user
to check their phone, not just the PC dev URL.

Fixed once the user was back at their PC, following the same elevated-command pattern established in
Phase 15: user ran `sc.exe stop CarTracker`, agent independently confirmed `STATE: 1 STOPPED` before
proceeding (not just trusted the user's report), agent ran `dotnet publish CarCareTracker.csproj -c
Release -o "C:/Services/CarTracker"` (scoped to the main project only - the bare `dotnet publish`
mistake from the original Phase 15 deployment was not repeated), confirmed the data folder was
untouched and the binary's timestamp updated, user ran `sc.exe start CarTracker`, agent independently
verified via curl against BOTH `127.0.0.1:5299` and the real `https://legion.tail80af14.ts.net`
Tailscale URL (not just localhost) that the new sidebar/widget markup was actually being served and
both real vehicles' data was intact.

**User confirmed live on their phone: "spot on."** This is the first genuine end-to-end visual
confirmation of Phase 16's work, closing out the phase for real - not just structurally complete, but
seen and approved by the user on the actual device they use it from.
