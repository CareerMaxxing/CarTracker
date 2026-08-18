# UI_TRANSITION.md

Tracking document for the Zara-inspired UI transition. Not a continuation of the original 14-phase
roadmap (that roadmap traced a locked spec, now fully delivered) — this is a separate, user-directed
initiative, same task-packet discipline as `PHASE_NN.md` files. Plan file: the transition plan
approved via Plan Mode covers Increments 1-7 as originally scoped; see the revision note below for
what changed after live review.

Prototype lives entirely in `data/themes/zara-study.css` (gitignored, activated via the app's
existing custom-theme mechanism — `UserTheme: "zara-study"` in `data/config/userConfig.json`) plus a
small number of structural CSS classes added to the tracked `wwwroot/css/site.css` with safe
Bootstrap-default fallbacks (so users on the stock theme are unaffected). A handful of Razor views
have real, tracked, additive edits (documented per increment below) — these are NOT undone by
reverting the theme.

## Revision note (after Increment 4 review)

User feedback: increments 1-4 read as a reskin (new colors/fonts/badges layered on Bootstrap's
untouched rounded/shadowed "boxed component" look), not an overhaul. Diagnosis confirmed by reading
the compiled `bootstrap.min.css` directly — buttons, modals, badges, dropdowns, and form controls all
derive their corner radius from a small set of root `--bs-border-radius*` variables via live `var()`
chains, meaning the boxed look could be (and should have been) flattened app-wide via a handful of
token overrides rather than fixed piecemeal per screen. Increment 5 was redefined around this
correction; increments 6+ renumbered accordingly (see roadmap below). Increment 4's Dashboard panel
shadow+radius treatment is superseded by Increment 5 (the panels now inherit the flattened tokens
automatically — no manual re-edit needed, confirming the token-driven architecture holds up).

## Revised roadmap

1. **Palette & type as an uploadable custom theme** — done
2. **Spotlight status role + structural primitives** — folded into increments 3-5 as needed rather
   than done as a standalone step (the `.status-badge-spotlight` role was introduced when Increment 3
   first needed it, not speculatively ahead of use)
3. **Garage grid card discipline** — done
4. **Dashboard tab & Plan kanban card discipline** — done (Dashboard panel elevation superseded by 5)
5. **Strip the boxed-component visual language, app-wide** — done
6. **Typographic and spatial confidence** — done
7. **Record tables & forms** (originally numbered 5) — done (CSS-achievable scope; see Open items for
   what still needs a library decision)
8. **Navigation & search** (originally numbered 6) — done
9. **Accessibility reconciliation** (originally numbered 7) — done for what's in-scope here (contrast
   verification, focus ring, frosted-background fix); broader accessibility work stays in Phase 14

## Increment log

### Increment 1 — Palette & type
- Self-hosted Fraunces (headings) + Work Sans (body), SIL OFL licensed, under `wwwroot/lib/fonts/`.
- New theme stylesheet overriding Bootstrap's `--bs-*` color variables for light/dark, monochrome
  `--bs-primary` (not red — Zara's own red is reserved for narrow emphasis, never primary actions).
- Verified: build, curl against `/css/theme.css` and rendered pages (Garage, modal, Dashboard tab,
  Service Records table), fonts confirmed serving.

### Increment 3 — Garage grid card discipline
- `Views/Home/_GarageDisplay.cshtml`: Year/Make/Model/identifier collapsed from 4 competing lines to
  eyebrow/title/meta hierarchy; Make+Model merged into one Fraunces title line.
- Reminder badge routed through a new `--ct-spotlight-*` accent role (independently tokenized from
  `--bs-danger` despite being the same red family, so "delete forever" and "needs attention" stay
  semantically separate) — the one deliberate accent moment per screen.
- Photo-overlay badge scrim tied to the palette's ink tone instead of hardcoded black.
- Hover changed from the app-wide `.card:hover { scale(1.05) }` to a calmer lift, scoped to
  `.garage-item` only.
- New structural classes added to `site.css` (tracked, safe fallbacks): `.status-badge-spotlight`,
  `.garage-metric-badge-spotlight`, `--ct-scrim-rgb` hook on `.badge.garage-metric-badge`.
- Verified: build, tests (10/10), curl against `/Home/Garage` with real vehicle data.

### Increment 4 — Plan kanban card + Dashboard tab
- `Views/Vehicle/Plan/_PlanRecordItem.cshtml`: 3 competing colored icon-badges (record type, reminder,
  priority) collapsed to 1 quiet icon + badges only for exceptions (Critical/Low get a `.status-badge`
  with real text; Normal gets none — badging the common case is noise). Reminder reuses the same
  spotlight role as the Garage card.
- Dashboard/Report tab panels given elevation (later superseded by Increment 5's flattening).
- Known gap, not fixed (flagged, not silently skipped): Chart.js charts on the Dashboard hardcode
  `useDarkMode ? "#fff" : "#000"` for text color, ignoring the palette. Real fix touches JS across 4+
  chart partials — deferred, candidate for Increment 6 or a dedicated pass.
- Verified: build, tests (10/10), curl against a real Plan record (Critical priority, rendered
  correctly with the new badge).

### Increment 5 — Strip the boxed-component visual language, app-wide
- `--bs-border-radius`, `-sm`, `-lg`, `-xl`, `-2xl`, and `--ct-radius-card` all set to 0. Deliberately
  NOT touching `--bs-border-radius-pill` — pill-shaped filter tags stay pill-shaped, an authentic
  fashion-retail contrast point rather than an inconsistency.
- `--ct-shadow-sm` (resting shadow) set to `none` — cards, Plan cards, and Dashboard panels are flat
  by default now. `--ct-shadow-md` (hover-only) kept and retinted to the ink color instead of generic
  black, as a lift cue on interaction, not decoration.
- `.btn-primary` given uppercase/tracked/weighted treatment (Zara's actual CTA voice), scoped to
  primary buttons only so it doesn't weigh down every secondary/icon button in the app.
- Pure CSS in the theme file — no rebuild required, ripples through every screen simultaneously
  (buttons, modals, badges, dropdowns, cards, panels) via the handful of token overrides.
- Verified: theme.css served live with the new tokens; confirmed pure static-file change (no
  `dotnet build` needed, server never restarted for this increment).

### Increment 6 — Typographic and spatial confidence
- `Views/Vehicle/Index.cshtml`: added a vehicle name header (`.lubelogger-vehicle-title`, "2004 BMW
  Z4") to the per-vehicle navbar. Real gap found, not assumed: the vehicle's identity previously
  appeared nowhere on the page itself, only in the browser tab `<title>`. Hidden below the `sm`
  breakpoint (existing pattern - `.lubelogger-tab` hides there too) to avoid crowding the mobile
  hamburger layout. Confirmed `checkNavBarOverflow()` in `shared.js` doesn't need changes - it
  dynamically measures navbar height and collapses tabs as needed regardless of what else occupies
  the row, so the new title just means slightly more gets collapsed into "..." on medium widths.
- Two type voices now used deliberately instead of one Fraunces-everywhere pass: Fraunces for
  names/titles/content headings, tracked-uppercase Work Sans for wayfinding/meta (tab labels, table
  headers). Tab labels moved OUT of the heading-font list into this pattern - nav chrome isn't a
  headline.
- New `--ct-space-7` generous spacing tier; applied to Garage card body and Dashboard panel padding.
- Verified: build, tests (10/10), curl confirms "2004 BMW Z4" renders on the vehicle page.

### Increment 7 — Record tables & forms
- Table headers get the tracked-uppercase treatment (Increment 6's pattern, extended here).
- Fixed the mobile stacked-row view's shadow: it hardcoded `rgba(0,0,0,.08)` directly rather than
  referencing `--ct-shadow-sm`, so it stayed boxed even after Increment 5 flattened everything else.
- Forms inherit Increment 5's flattened `--bs-border-radius` automatically (no separate work needed -
  `.form-control`/`.form-select` reference the same root variable).
- Scope note: this is the CSS-achievable slice for tables. Forms had a genuine library gap - see the
  Flatpickr migration entry below.

### Increment 7 (library swap) — bootstrap-datepicker → Flatpickr
User-approved dependency swap (bootstrap-tagsinput and the icon set were offered too and declined for
now - datepicker only). MIT-licensed, self-hosted under `wwwroot/lib/flatpickr/` (same vendoring
pattern as every other lib in that directory, no CDN, no build step).
- `Views/Shared/_Layout.cshtml`: swapped the `<link>`/`<script>` tags. `wwwroot/lib/bootstrap-
  datepicker/` removed entirely (confirmed zero remaining references first).
- Scope turned out much smaller than the initial 12-file grep suggested: nearly every call site
  funnels through two shared wrapper functions in `shared.js` (`initDatePicker`, `initExtraField
  DatePicker`), so rewriting those two internally to call `flatpickr()` instead of `.datepicker()`
  fixed every record-type form (11 JS files) with zero changes to those files.
- 3 remaining direct `.datepicker('getDate')` call sites (`shared.js` x2 pairs, `reports.js` x1 pair)
  replaced with a new `getPickerDate(input)` helper reading `input[0]._flatpickr.selectedDates[0]` -
  flatpickr has no jQuery-plugin-style `.datepicker('getDate')` API.
- New `flatpickrDateFormat()` helper translates the app's existing bootstrap-datepicker-style format
  token (`getShortDatePattern()` → `"dd/mm/yyyy"`) into flatpickr's own token syntax (`"d/m/Y"`) -
  translated, not hardcoded, so a future pattern change keeps working.
- The one genuinely custom piece: `garage.js`'s `initCalendar()` (the inline reminder calendar on the
  Calendar tab) used bootstrap-datepicker's `beforeShowDay` callback to inject reminder badges into
  day cells. Rewritten using flatpickr's `onDayCreate` hook with `inline:true` - same grouping logic,
  same timezone-offset handling, same generated badge HTML (`generateReminderItem` untouched).
- Flatpickr ships its own static, light-only, hardcoded-hex palette (`#569ff7` selected, `#fff`
  background, fixed 39px day cells) with nothing wired to `--bs-*` - unlike the table-shadow fix,
  there was no existing hook to route through, so this needed a real override: calendar background/
  border/radius, day-cell colors (default/hover/selected/today/disabled), month header in Fraunces,
  weekday labels in the tracked-uppercase pattern, nav arrows. The inline reminder calendar additionally
  needed full-width sizing and much taller day cells (default 39px is nowhere near enough for a stack
  of reminder badges) - scoped to `.reminderCalendarViewContent` so the popup-mode calendar used by
  ordinary date fields elsewhere keeps flatpickr's normal compact size.
- Removed now-dead CSS in `site.css` that targeted bootstrap-datepicker's table-based class names
  (`.datepicker-inline`, `.datepicker-days`, `.table-condensed` etc. - flatpickr uses a flexbox/span
  structure, not a `<table>`, so these selectors would never have matched anything).
- Verified: build (0 errors), tests (10/10), `node --check` on all 3 modified JS files (syntax valid),
  curl confirms flatpickr assets serve correctly and the old bootstrap-datepicker path 404s, a real
  Service Record add-form's date input renders with `initDatePicker($('#serviceRecordDate'))` intact
  and unchanged, reminder calendar partial renders with `initCalendar()` wired. Actual calendar-popup
  interaction and the reminder day-cell rendering need a live browser check - flagged, not assumed.

### Increment 8 — Navigation & search
- Global Search modal (`Views/Vehicle/Index.cshtml`, Phase 11) needed no dedicated markup work - a
  standard modal + large input, it already inherits every prior increment (flat corners, palette,
  type) automatically, and stays "obviously search-shaped" per the plan's explicit anti-Zara-pattern
  goal.
- Found and fixed: the app-wide focus ring (`site.css`, `.btn:focus`/`.form-control:focus`/etc.) was
  hardcoded to Bootstrap's stock blue/white (`#258cfb`/`white`), completely ignoring the palette.
  Routed through new `--ct-focus-ring-inner/-outer` tokens (fallback = the exact old hardcoded
  values, so stock-theme users see zero change); this theme sets them to `--bs-body-bg`/`--bs-primary`
  so the ring auto-adapts between light/dark without duplication.

### Increment 9 — Accessibility reconciliation
- Computed actual WCAG contrast ratios (not assumed) for every text/background pair in the palette,
  both themes: all 13 pairs checked pass AA (4.5:1) for normal text, most well past it (5.3-16.7:1).
- Found and fixed: `.frosted` (the fixed navbar's translucent blur background) was hardcoded to
  `rgba(33,37,41,.7)` / `rgba(255,255,255,.7)` - confirmed these were uncredited copies of Bootstrap's
  own default `--bs-body-bg-rgb`, so switching to `rgba(var(--bs-body-bg-rgb), .7)` is a zero-visual-
  change fix for stock-theme users while making it correctly theme-aware here (previously, content
  scrolling under the fixed navbar tinted stock grey/white instead of the warm palette).
- Broader accessibility work (icon-only button labels, keyboard nav, form labels, alt text) stays in
  Phase 14 rather than being duplicated here, per the original plan.

### Increment 10 — Garage/homepage rebuild toward the actual Zara homepage mechanic
User feedback: prior increments still read as a themed Bootstrap app, not something that feels like
the Zara homepage. Re-grounded in the teardown's own §03 finding rather than a generic "editorial
site" assumption: Zara's homepage opens on one full-bleed image, no grid, no overview - "the
homepage's job is mood-setting, not wayfinding." Deliberately did NOT copy that literally - a garage
tool that hid the vehicle overview for mood would be a functional regression, exactly the trade-off
the teardown's own closing line warns against ("where minimalism serves the brand versus where it
costs the user"). What transfers: oversized considered photography carrying more weight than
surrounding chrome, hover-reveal for browsing metadata (teardown §04 documents Zara's own listing
cards doing this for price/size).
- `Views/Home/_GarageDisplay.cshtml`: added a page-level "Garage" masthead title (same missing-title
  gap pattern as Increment 6's vehicle-name fix, found again at the top level) - large, alone, no
  subtitle copy (Zara's own homepage has no supporting text either).
- Vehicle photo height: 145px → 380px (260px on mobile), grid narrowed to 2-per-row (was 3) so each
  photo gets real width, not just height - a wide crop at 1/3-grid-width would have looked distorted
  for typically-landscape vehicle photos.
- Split the photo overlay into two independent layers instead of one crowded top banner: status
  signals that must stay visible regardless (sold, reminder-due) stay pinned top in
  `.vehicle-sold-banner`, untouched; browsing metadata (mileage, cost/mile, total cost, active
  projects) moved to a new bottom-anchored `.garage-metrics-reveal` that fades in on hover.
- Hover-reveal gated behind `@media (hover: hover) and (pointer: fine)` - touch devices have no hover
  concept, so metrics stay always-visible there rather than becoming unreachable. This was a
  deliberate check, not an afterthought: hiding data behind hover-only on a device that can't hover
  would itself be the "minimalism costing the user" failure mode the teardown warns about.
- Vehicle title (Make+Model) size increased 1.2rem → 1.6rem now that the card has real room.
- Verified: build (0 errors), tests (10/10), curl confirms the masthead renders, the 2-per-row grid
  is live, `garage-item-photo`/`garage-metrics-reveal` classes render on a real vehicle card. Hover
  fade and touch-fallback behavior need a live check - can't be verified without a browser.

### Increment 11 — Extending the rebuild: vehicle Dashboard, charts, every form, record tables
User said "continue to overhaul everything else." Applied the same lens (oversized/confident where
content is primary, quiet where it's meta, fix real bugs found along the way) to the highest-traffic
remaining surfaces rather than a screen-by-screen tour.
- `Views/Vehicle/Report/_ReportHeader.cshtml`: the 4 headline stats (odometer, distance, cost, MPG)
  promoted from Bootstrap's modest `.lead` (1.25rem, weight 300) to real confident Fraunces numbers
  (`clamp(1.8rem, 3vw, 2.6rem)`, tabular-nums so digits align across the row) with tracked-uppercase
  labels - this is a single vehicle's own "homepage" moment, same treatment logic as the Garage
  masthead.
- **Closed out the Chart.js color gap flagged since Increment 4**: 14 hardcoded `useDarkMode ? "#fff"
  : "#000"` instances across 4 chart partials (`_CostMakeUpReport`, `_GasCostByMonthReport`,
  `_MPGByMonthReport`, `_ReminderMakeUpReport`) replaced with two new `shared.js` helpers -
  `getChartTextColor()`/`getChartBgColor()` - that read the actual live `--bs-body-color`/`--bs-
  body-bg` via `getComputedStyle`, so chart text now matches whatever theme is active instead of a
  hardcoded literal that ignored the palette entirely. One inverse case in the gas-cost line chart
  (`backgroundColor: useDarkMode ? "#000" : "#fff"`) mapped to `getChartBgColor()` rather than being
  force-fit into the text-color helper.
- **Every form in the app, one fix**: found 203 bare `<label>` elements across `Views/` versus only 5
  using Bootstrap's own `.form-label` - labels sat directly against their inputs with only browser-
  default spacing. One CSS rule (`.modal-body label:not(.form-check-label)`) gives all of them the
  tracked-uppercase micro-label voice and real spacing, deliberately excluding `.form-check-label`
  since checkbox/switch text reads as a sentence ("Show Search"), not a field name.
- **Record tables**: default Bootstrap cell padding (`.5rem .5rem`) loosened slightly for breathing
  room without sacrificing the density this data genuinely needs. `td[data-column="description"]` -
  an existing convention already present on every record table, not new markup - picks up medium
  font-weight, since description is the actual content of a row and the rest (date, cost, odometer)
  is metadata; same primary-vs-quiet-meta pattern as the Garage title and the report stats above.
- Verified: build (0 errors), tests (10/10), `node --check` on `shared.js` (syntax valid), curl
  confirms the new stat classes render on a real vehicle, form labels render inside a real Add
  Service Record modal, and no hardcoded `#fff`/`#000` remain in any of the 4 chart files. Actual
  chart rendering couldn't be curl-verified against this dev environment's test vehicle (it has no
  cost/mileage data yet, so every chart partial falls into its empty-state branch) - flagged, not
  assumed working.

## Zara + Magneto overhaul (new plan, approved via Plan Mode — see plan file history)

User's read on increments 1-11: still looks/feels like a themed LubeLogger. Brought in Magneto (the
classic-car quarterly, art direction by Peter Allen) as a second research body alongside Zara. Full
plan and research grounding in the approved plan; synthesis rule: restraint governs structure, voice
governs a finite named list of moments - not a loosening of the existing discipline.

### Phase 0 — Identity purge
`Views/Shared/_Layout.cshtml`: `<title>`, `apple-mobile-web-app-title` — "LubeLogger" → "Car Tracker".
`wwwroot/manifest.json`: `"name"` likewise. `theme-color` meta tags deliberately left alone — they're
shared across every possible theme (stock or custom), hardcoding this theme's exact hex there would
break correctness for anyone not on this theme; reverted after initially changing them opportunistically.
Credits/Patreon/donation text in `Home/_Settings.cshtml` correctly left untouched (licence obligation,
not branding). Verified: build, tests (10/10), curl confirms all three identity strings live.

### Phase 1 — Typography: verified the axes, then used them at named moments only
Inspected the self-hosted `Fraunces-Variable.woff2` with `fontTools` (installed for this) — confirmed
the suspicion: only `opsz`+`wght` axes present, `SOFT`/`WONK` missing (Google's default CSS2 delivery
serves a reduced axis subset). Re-sourced the full 4-axis build directly from the type foundry
(`github.com/undercasetype/Fraunces` v1.0 GitHub release), re-verified with `fontTools` that SOFT/WONK
are now actually present, replaced the vendored file. Applied `font-variation-settings: "opsz" 144,
"WONK" 1, "SOFT" 25` to exactly three named elements — `.garage-masthead-title`, `.lubelogger-vehicle-
title`, `.report-stat-value` — and nowhere else; the app wordmark moment is deferred to Phase 6b
(no text wordmark element exists yet, only an image logo). Verified: font file serves correctly
(195KB vs. the old 67KB axis-reduced file), CSS scoped to exactly the three intended selectors.

### Phase 2 — Colour: chart series palettes retuned
Found and fixed the two hardcoded stock-rainbow Chart.js segment arrays (separate, still-open surface
from increment 11's text/bg colour fix): `_CostMakeUpReport.cshtml` and `_ReminderMakeUpReport.cshtml`.
Retuned to a muted mid-century palette in the theme's own register; the reminder chart's semantic
escalation order (not-urgent → urgent → very-urgent → past-due) preserved exactly, only the hex values
changed. Computed WCAG contrast for all 9 new colours against both paper/ink backgrounds (non-text
3:1 minimum) — iterated twice on 3 borderline values until every colour cleared 3:1 against both.
Optional chrome accent moment (Phase 2.2 in the plan) not done this pass — deferred, not declined.
Verified: build, tests (10/10), curl confirms the new colours render in a real vehicle's cost chart.

### Phase 3 — Documents: rebuilt from a bare table into a considered grid
`Views/Vehicle/Documents/_Documents.cshtml` had zero thumbnails before — genuine payoff, not a
reskin. New anatomy: thumbnail (real `<img>` for image attachments via the existing `StaticHelper.
GetAttachmentIsImage`, an icon tile for everything else), bold filename, record-type + date as an
excerpt line, small category badge on the thumbnail — Magneto's own article-index anatomy mapped
directly onto a screen it fits almost exactly. New `filterDocumentsGrid()` in `shared.js`, kept
deliberately separate from the shared `filterTable()` (used by 11 other still-tabular record views)
rather than generalizing that function and risking the other 11. Parts/Odometer stay tabular,
explicitly, per the plan's own contrast case. Verified: build, tests (10/10), curl against real
document data (2 real PDF attachments on the dev vehicle) confirms thumbnails, category badges, and
excerpt lines all render correctly.

### Phase 4 — Icon set: Bootstrap Icons → Phosphor Icons
Grepped every `bi-*` class actually used across `Views/` and `wwwroot/js/`: 105 distinct icons (not
446 — that was total usage count, not distinct icons). Fetched Phosphor's real codepoint mapping
(`phosphor-icons/web`'s `style.css`, 1530 icons parsed), hand-curated all 105 to their closest
Phosphor equivalent by actual meaning, verified computationally that every chosen target name exists.
Checked the one case where fill-vs-outline looked load-bearing (the reminder bell, `vehicle.js`) and
confirmed the active/inactive state is already carried redundantly by a colour class and a shake
animation, not icon shape alone — safe to map every `-fill` variant to Phosphor's plain regular style
rather than self-hosting a second font family. CSS-only override layer (self-hosted Phosphor webfont,
`@font-face` + a `[class^="bi-"]::before, [class*=" bi-"]::before` font-family swap + 105 individual
codepoint remaps), matching Bootstrap Icons' own selector pattern and `!important` to win the
specificity tie — zero Razor/JS edits, confirmed by design (Bootstrap Icons' base rule already applies
to every `bi-*` element via attribute selector, not a fixed list). Verified: build, tests (10/10),
brace-balance check on the theme file, curl confirms the font serves and spot-checked icons resolve
to their intended codepoints.

### Phase 5 — Motion: reduced-motion gating + one restrained reveal
`site.css`: added the standard universal `prefers-reduced-motion: reduce` snippet (0.01ms animation/
transition durations, not `none` — end-states like a hover-lift's final position or a reveal's final
opacity still apply, nothing here depends on a `transitionend`/`animationend` event ever firing).
App-wide, not theme-specific — an OS-level accessibility preference shouldn't depend on which theme
happens to be active. Covers everything found: `.bell-shake`/`.tablerow-shake` (real keyframe shake
animations, confirmed actively used in `vehicle.js`/`shared.js`, not dead code), the app-wide `.card`
hover scale, and every hover/reveal effect the theme file adds on top.

For the "one restrained reveal," reconsidered the original IntersectionObserver plan after finding a
real timing hazard: both named elements (Garage masthead, vehicle title) are always above the fold —
there's nothing to scroll into view — and both render via AJAX injection well after any
`DOMContentLoaded`-scoped observer setup would already have run. Used a CSS `@keyframes` fade-up
animation instead, gated by its own explicit `prefers-reduced-motion` block — triggers correctly
regardless of when the element enters the DOM, no JS wiring, no timing risk. Verified: brace-balance
check, curl confirms both CSS additions are live.

### Phase 6a — Nav de-duplication (scope revised after reading all 6 files in full)
Original plan assumed 6 files shared one duplicated pattern. Reading `Admin/Index`, `API/Index`,
`Home/Setup`, and `Migration/Index` in full found they actually share a near-identical **simple
title-bar** nav (logo + static title, no tab strip) — genuinely safe, low-risk to unify. `Home/Index`
and `Vehicle/Index` use a structurally different, more complex tab-strip pattern (`TabOrder`-driven
tabs, the reminder bell's special icon markup, a user/admin dropdown, a separate mobile nav that
duplicates the same tab list again) with real per-view logic that doesn't share cleanly — forcing
both patterns into one partial would have meant either an unreadable parameter surface or under-
abstracting and leaving most of the actual duplication in place. Scoped Phase 6a to the clean win:
- New `Views/Shared/_SimpleNavBar.cshtml`, tuple-modeled (`(string Title, string? HelpText)`, matching
  this codebase's existing tuple-model convention rather than adding a new C# class for one small
  partial). All 4 calling views now call `@await Html.PartialAsync("_SimpleNavBar", (...))` instead of
  independently hand-writing the markup.
- Preserved exact pre-existing behaviour, including a likely bug: API's title ("API") and help text
  are hardcoded English, never run through `translator.Translate()` in the original code — kept
  exactly as-is rather than silently "fixing" translation coverage that wasn't in scope.
- `Home/Index` and `Vehicle/Index` deliberately left untouched — noted as a real, intentional scope
  boundary, not a gap that was missed.
- Verified: build initially failed (`IConfigHelper` not found — the new partial needed its own
  `@using CarCareTracker.Helper`, not inherited from `_ViewImports.cshtml`), fixed, then 0 errors,
  10/10 tests. Live-rendered and curl-confirmed correct output for 2 of 4 views (`/API`, `/setup`).
  The other 2 (`/Admin`, `/Migration`) redirect to `/Error/Unauthorized` in this dev environment for
  reasons confirmed unrelated to this change by reading the actual controller source: `EnableAuth:
  false`'s auto-generated identity (`Middleware/Authen.cs`) only carries the `IsRootUser` role, and
  `AdminController` requires `IsAdmin` specifically; `MigrationController`'s `Index()` action redirects
  unconditionally when no Postgres connection is configured (this environment uses LiteDB) - both
  pre-existing behaviours, confirmed by reading the code, not assumed. Their Razor syntax is still
  confirmed valid (the build compiles every view regardless of whether a request ever reaches it, and
  a syntax error in either file would have failed the whole build) - live rendering just isn't
  reachable via curl in this specific dev setup.

### Phase 6b — Nav visual rebuild
The single biggest remaining "looks like LubeLogger" signal turned out to be literal, not just
stylistic: the navbar logo (`GetLogoUrl()`/`GetSmallLogoUrl()`) defaults to `wwwroot/defaults/
lubelogger_logo*.png` — actual LubeLogger-branded image files, untouched by every prior increment
because they were all CSS/theme work. Fixed with a typographic wordmark ("Car Tracker", Fraunces, the
same hero axis treatment as the other 3 named moments from Phase 1 - always intended to include a
wordmark once one existed, per that phase's own comment) rather than commissioning a graphic mark
that doesn't exist. Respects a real admin-uploaded custom logo where one is set - compares the
resolved URL against `StaticHelper.DefaultLogoPath`/`DefaultSmallLogoPath` (the same comparison the
codebase's own logo-upload flow already uses in `ConfigHelper.cs`) and only substitutes the wordmark
when the default is still active, in `Views/Shared/_SimpleNavBar.cshtml`, `Views/Home/Index.cshtml`
(both the desktop and mobile-breakpoint logo slots), and `Views/Vehicle/Index.cshtml` (only in the
fallback branch - a vehicle's own photo still takes priority when `ShowVehicleThumbnail` is on,
unchanged).
- `checkNavBarOverflow()`'s hardcoded `48px` threshold (`shared.js`) - the phase's first non-optional
  acceptance criterion - satisfied by construction rather than live re-derivation: `.ct-wordmark-wrap`
  carries `min-height:48px` + flex-centering so the navbar row's height stays identical to when the
  48px-tall logo image occupied that slot, regardless of the wordmark text's own natural size. Chosen
  specifically because this environment can't verify a new threshold value live.
- Added a considered active-tab indicator (an underline in `--bs-primary`, not Bootstrap's default
  border-bottom trick) - previously the active tab had no visual signal at all beyond default text
  colour, since `site.css`'s own `.lubelogger-tab` rules already strip Bootstrap's border/background
  active-state styling. Directly answers the second acceptance criterion and one of the Zara
  teardown's own named weaknesses (§10 - clickable/active vs. static text looking identical).
- Re-walked the rest of Zara's documented-weaknesses checklist rather than assuming it still holds:
  `.frosted`'s theme-aware background (fixed in increment 9) re-confirmed unchanged; Global Search and
  the Garage inline search both already use a labelled input + magnifying-glass icon button, already
  unambiguously search-shaped before this phase, untouched by it.
- Verified: build (0 errors), tests (10/10), brace-balance check, curl confirms the wordmark renders
  correctly and respects the vehicle-thumbnail-priority branch on `/Vehicle/Index`, and correctly
  falls back to an uploaded custom logo's `<img>` tag when one is set (verified by reading the
  conditional logic against `ConfigHelper`, not just the default-path case). The active-tab underline
  and the wordmark's actual visual weight/placement are exactly the kind of thing this environment
  cannot confirm without a browser - flagged, not assumed correct.

### Critical fix (found from a live screenshot, not any curl/build check) — buttons rendered stock Bootstrap colors this entire time
User sent a screenshot of the live Dashboard for the first time this session and asked "is this the
design?" It mostly was (Fraunces stat numbers, tracked-uppercase tabs, active-tab underline, the new
chart palette all confirmed working) - but a bright stock-blue button in the Collaborators panel
didn't belong anywhere in this palette. Investigated rather than guessing: read the compiled
`bootstrap.min.css` directly and found the actual root cause - `.btn-primary` (and every other
`.btn-*`/`.btn-outline-*` variant) does **not** derive from the root `--bs-primary`/`--bs-danger`/etc.
custom properties at all. Each button class defines its own separate, independently-hardcoded
`--bs-btn-bg`/`--bs-btn-border-color`/etc., baked in as literal hex at Bootstrap's own build time -
confirmed directly in the source (`.btn-primary{--bs-btn-bg:#0d6efd...}`), not a `var()` reference.
This is exactly the failure mode curl/build verification structurally cannot catch: the HTML class
name (`btn-primary`) is correct regardless of what colour the browser actually paints, so this had
been silently rendering stock Bootstrap colours on every button in the app since Increment 1,
undetected across 11 increments and 6 further phases until an actual screenshot surfaced it.

Traced the same root cause (component-local hardcoded variables instead of root `var()` references)
to four more places by reading the compiled CSS systematically rather than assuming buttons were the
only casualty: `.form-check-input:checked`/`:indeterminate` (checkboxes/switches - 42 files use
these), `.dropdown-menu`'s active-item highlight, `.form-range`'s slider thumb, and `.nav-pills`. Also
found `--bs-success`/`--bs-info` had never been retuned at all in this entire body of work -
`.status-badge-success` (used by the Government Data panel's Taxed/Valid badges, visible in the same
screenshot) had been rendering stock Bootstrap green this whole time too, just not jarringly enough
to notice by eye.

Fixed all of it together in one place, once, rather than piecemeal:
- Added full `--bs-success`/`--bs-info` definitions (base, rgb, bg-subtle, border-subtle, text-
  emphasis) to both light and dark root blocks, same muted-premium register as the rest of the
  palette, contrast-verified (all pairs pass AA, 4.65-9.64:1).
- Every `.btn-*`/`.btn-outline-*` variant now overrides its local `--bs-btn-*` properties to
  reference the theme's own variables. Hover/active shades use `color-mix(in srgb, var(--bs-X) 85%,
  var(--bs-body-color) 15%)` instead of ~24 hand-picked hex values - mixing in body-color darkens in
  light mode (where body-color is ink) and lightens in dark mode (where body-color is paper)
  automatically, one rule instead of duplicated light/dark blocks. Button text uses `var(--bs-body-
  bg)` for the same reason it's correct in both themes: every variant colour in this palette is
  deliberately deep/saturated in light mode and light/desaturated in dark mode, so the page's own
  background colour is always the right contrasting text colour, in both directions.
- `.form-check-input:checked`/`:indeterminate`, `.form-range` thumb, `.dropdown-menu` active-item, and
  `.nav-pills` active state all now reference `var(--bs-primary)` instead of the hardcoded `#0d6efd`.

Also fixed in the same pass, found in the same screenshot: `Helper/StaticHelper.cs`'s
`GetBarChartColors()` - a hardcoded 12-step cost-intensity gradient (low cost -> high cost, used by
the Dashboard's monthly expenses bar chart) that was plain C#, not CSS, so no theme-file audit would
ever have surfaced it. Retuned to the same teal-to-rust progression as the Cost Breakdown pie chart's
palette (Phase 2) rather than the original stock green-to-red gradient, preserving the exact same
12-step cost-rank logic, contrast-verified against both backgrounds. And a genuine duplication this
session introduced without noticing: `Views/Vehicle/Index.cshtml` had a pre-existing "click to edit
this vehicle" element on the right of the navbar that displays the vehicle's full name and
identifier - Phase 1's new `.lubelogger-vehicle-title` on the *left* of the navbar was added without
checking whether something already showed that same information elsewhere on the page. Fixed by
keeping the pre-existing element's edit functionality but dropping the redundant repeated text,
leaving a single icon-only edit affordance (matching the `link-body-emphasis` icon-action pattern
already used elsewhere in the app, e.g. the attachment preview modal).

Verified: build (0 errors), tests (10/10), brace-balance check, curl confirms the new chart-color
values render on the real dashboard and the duplicate vehicle-name text is gone from `/Vehicle/Index`
while the edit icon remains. The actual rendered button/checkbox/dropdown colours still need the same
live confirmation as everything else in Phase 6b - flagged as the next thing to check, not assumed
fixed just because the CSS is now theoretically correct.

## Magneto print study (direct PDF research, not secondary sources)

User provided the actual Spring 2024 issue (220 pages) after finding the earlier web-only Magneto
research still produced a reskin, not a structural translation. Read every page via contact sheets
(11 sheets of ~20 thumbnails each, built with PyMuPDF/Pillow since the 142MB file exceeded the
Read tool's direct PDF limit), then full-resolution deep-dives on the richest examples. Concrete,
directly-observed findings (not paraphrased from secondary sources):
- A consistent hairline vertical-rule column grid underlies every page type (contents, editor's
  letter, contributors, feature spreads).
- Contents page (p.10): page numbers render *larger than the headlines next to them* - numerals as
  the dominant visual anchor, not a supporting label.
- Headline position is genuinely NOT fixed - p.22 staggers the headline below a full-width photo,
  sharing a row with body copy already running; p.38 puts it right after the byline. Same grid,
  different rhythm, because the content needed it.
- Confirmed from the masthead itself (p.16): "Creative director Peter Allen... creating five new
  typefaces. Each feature has a specially designed custom style."
- The Senna feature (p.61-80) repeats a "chapter divider" pattern for each interviewee: full-bleed
  dark-teal page, the person's name in bold two-tone custom type, and a huge fragment of Senna's own
  "S" letterform reused as a unifying graphic motif across every subject's page.
- The Mangusta feature (p.82-96) overlays massive white display type directly onto studio photos
  with a dramatic shadow/glow, integrated into the photo composition rather than placed near it.
- "Top 50 Group C Cars" (p.168-180): a genuine countdown-list module - huge rank number as the
  dominant element, small thumbnail as support - the right reference for a *comparison/index*
  context, not the varied-photo-crop treatment (that's specific to a single feature's own images).
- p.60 ("GODSPEED"): a full-bleed poster page, pure stacked geometric type in stepped-tone colour
  bands, zero photography - proof the issue is willing to let typography alone carry a whole page.
- Honest limitation: WebFetch converts pages to markdown before analysis, stripping all CSS/JS - so
  magnetomagazine.com's actual hover/interaction states are structurally invisible to available
  tools. Interactive-state design had to be originated, not copied, and this was stated plainly
  rather than fabricating a finding.

### Applied so far
- **Garage masthead**: vehicle count rendered as a huge numeral (`.garage-masthead-count`, same hero
  font-variation-settings as the other named moments) beside "Garage", hairline rule between them -
  directly reusing the contents-page pattern, not just "a big number somewhere."
- **Garage card index**: each vehicle card gets a small numeral prefix (`.garage-item-index`) -
  scoped correctly to the countdown-list reference (comparison context, uniform treatment) rather
  than the photo-essay varied-crop reference, after re-checking which pattern actually applied.
- **Vehicle Dashboard hero** (`Views/Vehicle/Report/_Report.cshtml`, `ReportViewModel.
  VehicleImageLocation`/`VehicleIsSold` added, set in `Controllers/Vehicle/ReportController.cs`): a
  full-width photo band opens the Dashboard, before the stat/chart grid - the closest real analog to
  a Magneto feature (a long-form piece about one specific car) gets the same photo-before-data
  opening move every feature in the issue uses. Deliberately does NOT overlay the vehicle's name on
  the photo (unlike Mangusta's text-over-image treatment) - that identity already lives in the nav
  bar's `.lubelogger-vehicle-title` directly above it; repeating it would be redundant. Skipped
  server-side entirely when the vehicle has no real photo (checked against the literal default-
  placeholder path), rather than showing an oversized generic image.
- Verified: build (0 errors), tests (10/10), brace-balance checks, and curl confirms both the
  masthead numeral and the Dashboard hero band render against real data - the test vehicle now has
  an actual uploaded photo (added during the user's own live testing), so the hero band's "has a
  real photo" branch got a genuine end-to-end check, not just logic inspection.

### Phase 2 — Pull quotes
Applied to the shared `.ct-empty-state` primitive (`site.css`, used by Documents/Parts/Government
Data - 3 call sites, zero markup changes needed). Was plain small `--bs-secondary-color` text; now
large Fraunces type with a short anchoring rule beneath, echoing p.38's pull-quote device. Verified:
brace-balance, CSS live-served. Could not curl-verify the actual rendered empty state - all 3 call
sites currently have real data in this dev environment (the user has been adding real test data
during live review), so this is confirmed correct by construction (existing, already-adopted
primitive, pure CSS) rather than by seeing it render.

### Phase 3 — Poster-style empty Garage state
The fully-empty Garage (`Views/Home/_GarageDisplay.cshtml`'s `else` branch, `Model.Any()` false) was
a bare grey dashed box - the least considered moment in the app, and the very first thing a new user
ever sees. A literal 2-word stacked GODSPEED-style headline needed invented, untranslated copy (no
existing key fit that shape) - used the real modal title string ("Add New Vehicle", already
translated, from `Views/Vehicle/_VehicleModal.cshtml`) instead of inventing text, and applied the
actual lesson from p.60 (confident solid colour, zero photography needed) rather than forcing a
literal template match. Uses `--ct-spotlight-solid`, not `--bs-primary` - correctly the same "needs
your attention" register as the reminder badge, not a routine button. Could not curl-verify the
live render - the dev vehicle has real data, and deleting it to force the empty state would mutate
the user's test data without a clear reason to.

### Phase 4 — Interactive/hover language
Since magnetomagazine.com's actual CSS is structurally invisible to WebFetch, this had to be
originated rather than copied. Found a genuine, real gap by reading Bootstrap's compiled CSS
directly: `.link-body-emphasis` (every icon-only action in the app - vehicle-edit icon, attachment-
preview's close/copy-link icons, others) has zero hover or focus state in Bootstrap's default - a
static colour with no feedback that it's interactive until clicked. The same "clickable vs. static
text" failure the original Zara research flagged (teardown §10), just found in this app's own icon
buttons. Fixed with a colour-only transition (no motion) to `--bs-primary` on hover/focus-visible.
Verified: CSS live-served, `!important` correctly matched to override Bootstrap's own `!important`
on this specific rule (confirmed by reading the compiled source, not assumed).

### Phase 5 — Chapter-divider motif, applied only where it genuinely fits
Honest assessment: the Senna feature's chapter-divider pattern (full-bleed colour, reused giant
letterform motif, marking named sub-sections within one larger story) doesn't map cleanly onto most
of this app - Car Tracker has no sub-narratives within one feature the way a magazine profile
interviews multiple people about one subject. Forcing it everywhere would be pattern-matching, not
translation. The one place it genuinely fits: a sold vehicle's Dashboard is a real "chapter closed"
moment, previously signalled only by a grayscale photo filter with no caption. Added `.report-hero-
sold-band` - a solid ink band anchored to the hero photo's bottom edge, bold "SOLD" + the actual sold
date (`ReportViewModel.VehicleSoldDate`, added alongside `VehicleIsSold`, set in
`Controllers/Vehicle/ReportController.cs`). Verified: build (0 errors), tests (10/10), CSS live-
served; could not curl-verify the actual sold-state render since the dev vehicle isn't marked sold,
and marking it so purely for a test would mutate real data without a clear enough reason.

### Session-wide honest gap
Multiple Phase 2/3/5 pieces are verified correct by construction (build success, live CSS serving,
careful reading of existing conditional logic) rather than by seeing them actually render, because
this dev environment's one test vehicle now carries enough real data (added during the user's own
live testing) that several of the states this work targets - empty lists, a sold vehicle, an
entirely empty Garage - aren't currently reachable without mutating that data. Flagged explicitly
rather than claimed as verified; a live browser check with a second test vehicle (or one temporarily
marked sold) would close this gap.

## Promotion to permanent default (finalization)

User signalled the whole initiative is done ("we are finalising on this, wrap it up"). Final step:
promote the prototype theme file into the app's real, permanent default rather than leaving it as an
opt-in `UserTheme` selection.

- Merged `data/themes/zara-study.css` (1079 lines, gitignored, held every increment/phase's CSS since
  it was easiest to iterate on as a fully-reversible layered theme) onto the end of the tracked
  `wwwroot/css/site.css` (was 1031 lines). Backed up first (`site.css.bak`), removed once the merge
  was verified.
- Bug caught before any build/test was run: the merge script's `tail -n +2` only skipped one line of
  `zara-study.css`'s own old multi-line header comment, leaving 5 lines of a dangling, already-closed
  comment fragment sitting as raw invalid CSS right after the new provenance header. Fixed via direct
  edit; re-verified brace balance (413 open / 413 close) before touching build/test.
- Reset `data/config/userConfig.json`'s `UserTheme` from `"zara-study"` to `""` (gitignored, no git
  impact) — the design is now baked into `site.css` itself, so no theme selection is needed and
  leaving the old value in place would have double-loaded identical rules through `/css/theme.css`.
  Deleted `data/themes/zara-study.css` itself once its content was confirmed fully merged, to remove
  any ambiguity about which file is the source of truth going forward.
- Full verification after the fix, against this exact final state: `dotnet build` (0 errors,
  0 warnings), `dotnet test Tests/CarCareTracker.Tests.csproj` (10/10 passing), dev server restarted
  fresh, then curl-confirmed: `/css/site.css` alone now serves the complete 2119-line merged system
  (200 OK, Fraunces `@font-face` present, no trace of the dangling-comment bug); `/css/theme.css`
  correctly serves 200 OK with 0 bytes (no theme selected, nothing left for it to layer); `/`,
  `/Home/Garage`, `/lib/fonts/Fraunces-Variable.woff2`, and `/lib/phosphor/Phosphor.woff2` all serve
  200; the Garage AJAX partial (`/Home/Garage`) renders every expected class from this whole
  initiative in one response — masthead, masthead count, per-card index numeral, hover-reveal metrics,
  sort icon, context menu. This is the first time the *complete, final* merged state (post dangling-
  comment fix) has been build/test/curl-verified — earlier verification in this log was against the
  in-progress theme file, not this final artifact.
- `site.css.bak` removed after the above verification passed.

This closes out the reversible-prototype architecture entirely: there is no longer a `UserTheme`
this design depends on, and `data/themes/` no longer holds anything related to this initiative. Any
future UI work should treat `site.css` as the real, permanent source of truth and either edit it
directly or use the theme-upload mechanism for genuinely separate, user-facing alternate themes (its
original purpose), not as a staging area for default-design work.

## Known open items

- **bootstrap-tagsinput** (vehicle tags, filter chips) still the stock plugin, still visually
  independent of `--bs-*` — not revisited this pass either.
- **Phase 2's optional chrome accent moment** — not done, deferred rather than declined.
- **`Home/Index`/`Vehicle/Index`'s tab-strip nav markup itself stays unshared** (Phase 6a's deliberate
  scope boundary) - Phase 6b's visual treatment (wordmark, active-tab underline) was applied to both
  independently since the CSS/logo-conditional pattern is small enough to duplicate safely; a future
  pass could still unify the full tab-list rendering logic if the duplication becomes a real
  maintenance cost, but it wasn't forced here.
- **Session-wide honest gap items above (Phase 2/3/5 empty/sold states)** remain verified-by-
  construction rather than by live render — still true after promotion; nothing about baking the CSS
  into `site.css` changes what's reachable in this dev environment's current data.
