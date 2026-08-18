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
