# PHASE_02 — UI Design System

## Task packet (this increment)

```
TASK ID: PHASE-02-01
TITLE: Design token foundation + CSS-only component refresh
OBJECTIVE: Establish the target design system (docs/UI_SPEC.md) and land its first layer as a
  CSS-only change, without touching any interactive markup, so it can be verified for correctness
  without a browser/screenshot tool (not available in this environment — see note below).
INPUTS: docs/UI_INVENTORY.md (Phase 0), docs/REQUIREMENTS.md (Phase 1), the original spec's Phase 2
  bullet list (shell/nav, typography/spacing, cards/buttons/forms/tables/dialogs, status badges,
  loading/empty/error states, responsive layout, dark/light theme, vehicle context header).
ALLOWED SCOPE: docs/UI_SPEC.md; wwwroot/css/site.css; wwwroot/css/loader.css. No .cshtml changes,
  no JS changes, no changes to any interactive/tab/navigation markup.
NON-SCOPE: Restructuring the shell/navigation markup (triplicated tab-bar rendering found in
  Home/Index.cshtml, Vehicle/Index.cshtml, and similarly in Kiosk/Migration/Setup/Admin/API views);
  adopting the new status-badge/empty-state primitives into any actual view; typography scale
  changes beyond what's already token-driven; anything requiring visual/interactive verification
  this task can't perform.
IMPLEMENTATION REQUIREMENTS:
  - Design tokens (spacing/radius/shadow/motion) as CSS custom properties, referencing Bootstrap's
    own --bs-* variables rather than duplicating hardcoded colors, so dark/light theming and any
    user-uploaded custom theme keep working unchanged.
  - Apply tokens to existing duplicated-value rules (.card, .taskCard, .kiosk-card:hover) with
    values kept numerically identical to before, so there's no unverified visual change there.
  - Add new opt-in primitives (.status-badge family, .ct-empty-state) per UI_SPEC.md, unused by any
    view yet.
  - Fix any theme-correctness bug found along the way (found: .sloader/.loader hardcoded white,
    broken in dark mode).
DELIVERABLES: docs/UI_SPEC.md, updated wwwroot/css/site.css, updated wwwroot/css/loader.css.
ACCEPTANCE CRITERIA:
  - [x] docs/UI_SPEC.md exists and documents tokens, components, states, responsive/interaction
        rules, and explicitly what was and wasn't touched.
  - [x] dotnet build: 0 errors, same warning count as before (209) confirming no product code
        drift.
  - [x] App starts and serves / and the two modified CSS files at 200, verified via curl.
  - [x] New CSS variables (--ct-space-1, bs-body-bg-rgb reference) confirmed present in the served
        output, not just the source file.
  - [x] Bootstrap variable names referenced (--bs-body-bg-rgb, --bs-emphasis-color) confirmed to
        actually exist in the bundled Bootstrap 5.3.2 build before relying on them.
  - [x] No .cshtml file touched; zero risk to interactive/tab/navigation behavior.
VALIDATION COMMANDS:
  dotnet build
  dotnet run --urls http://localhost:5299
  curl :5299/, :5299/css/site.css, :5299/css/loader.css (all 200)
  curl :5299/css/site.css | grep -c -- "--ct-space-1"   (present)
STOP CONDITION: Token foundation landed and verified server-side; further visual rollout requires
  either a way to visually verify (browser/screenshot tool, or human-in-the-loop review) or
  restricting to more CSS-only, non-interactive changes. Stopping here to report and ask how the
  user wants to proceed with the higher-risk nav/shell restructuring and per-view rollout.
```

## Why this phase is scoped smaller than the original spec's full bullet list

This environment has no browser/screenshot tool available, so visual quality (does it actually
*look* good) can't be verified directly — only structural correctness (builds, serves, doesn't
break markup) can be. `Vehicle/Index.cshtml`'s navigation alone renders its 13-tab list **three
times** (desktop strip, "more" overflow dropdown, full-screen mobile off-canvas panel), each block
wired to `vehicle.js` tab-activation/AJAX logic via specific element IDs. Restructuring that markup
without being able to click through the result in a browser would be exactly the kind of
"unverified major change" `CLAUDE.md` warns against — a mistake there breaks navigation for the
whole app, not just a visual regression.

Instead, this increment stays entirely inside `wwwroot/css/*.css` — a single, well-understood file
type with no interactive behavior, verifiable by (a) confirming referenced Bootstrap variables
actually exist in the bundled build, (b) keeping every value applied to existing selectors
numerically identical to what was there before (pure refactor, not a value change), and (c) only
introducing new visual behavior where it's an unambiguous bug fix (the dark-mode loader issue) or
net-new, unused-so-far primitives that can't regress anything because nothing references them yet.

## What was done

1. Read `_Layout.cshtml`, `site.css`, `loader.css`, and the `@section Nav` blocks in
   `Home/Index.cshtml` and `Vehicle/Index.cshtml` to understand the current shell/nav structure
   before deciding what was safe to touch.
2. Found the nav is triplicated per top-level view (confirmed via `Vehicle/Index.cshtml:32-195`) —
   confirmed this needs interactive verification and deferred it rather than restructuring blind.
3. Found a real, unambiguous bug along the way: `wwwroot/css/loader.css`'s `.sloader` overlay was
   hardcoded `rgba(255,255,255,0.5)` and `.loader`'s spinner border hardcoded `white` (including
   mid-animation in `@keyframes spin`) — in dark mode this produces a white veil with a
   near-invisible white-on-white spinner. Fixed using the same `rgba(var(--bs-*-rgb), alpha)`
   pattern already used elsewhere in `site.css`, confirmed the referenced Bootstrap variables
   (`--bs-body-bg-rgb`, `--bs-emphasis-color`) actually exist and correctly flip per theme in the
   bundled Bootstrap 5.3.2 build before relying on them.
4. Wrote `docs/UI_SPEC.md` — the target design system, explicit about what's in scope now vs.
   deferred.
5. Added a design-token layer (spacing/radius/shadow/motion) to `site.css`, applied it to
   `.card`/`.taskCard`/`.kiosk-card:hover` with values kept identical to before (pure
   consolidation, not a visual change), and added two new opt-in primitives
   (`.status-badge` family, `.ct-empty-state`) that no view uses yet.
6. Verified: `dotnet build` (0 errors, same 209 warnings as Phase 0/1 baseline — no product code
   touched), ran the app, confirmed `/`, `/css/site.css`, `/css/loader.css` all return 200, and
   confirmed the new CSS actually reaches the served output (not just the source file).

## Increment 2: shell/navigation consolidation (2026-08-17, same day)

The user chose "review locally as I go" for continuing Phase 2 (see STATE.md decision log). With
that workflow in place, the triplicated tab-bar markup flagged as out-of-scope above was revisited:

1. **`Views/Home/Index.cshtml`** (Garage page, 4 tabs: Garage/Supplies/Calendar/Settings) —
   deduplicated the tab list feeding the desktop bar and its "more" overflow dropdown into a single
   Razor source list. Verified by diffing rendered HTML before/after: only whitespace and one
   harmless `aria-selected` addition differed. User confirmed in a live browser: page loads,
   overflow-to-dropdown behavior works, dark mode works.
2. **`Views/Vehicle/Index.cshtml`** (vehicle detail page, 13 tabs) — same treatment, at three-way
   scale (desktop bar / "more" dropdown / mobile off-canvas panel, 39 hand-written items down to
   one 13-item list). Verified against a throwaway test vehicle (created and deleted via the API;
   the user's real vehicle was untouched) by diffing whitespace-normalized rendered HTML. The first
   pass caught a real bug — a Razor whitespace-as-text quirk introduced stray spaces around mobile
   nav labels — fixed and re-verified clean. User confirmed in a live browser: all 13 tabs switch
   correctly, overflow dropdown works at this scale, mobile off-canvas menu (including the
   Reminders bell icon) renders correctly.
3. **Audited the remaining four `@section Nav`-bearing views** (`Migration/Index.cshtml`,
   `Admin/Index.cshtml`, `API/Index.cshtml`, `Home/Setup.cshtml`) for the same pattern — none of
   them have it. Each is a simple single-line title bar with no duplicated tab markup. The
   triplication Phase 0 flagged only existed in the two views now fixed; there is no more of this
   specific structural issue elsewhere in the codebase.
   (`Home/Setup.cshtml` does have a smaller, different duplication — its setup-wizard step nav
   renders as a desktop button row and a separate mobile `<select>` dropdown, 6 items each. This
   is a distinct, lower-risk pattern, not the `checkNavBarOverflow()`-driven tab system, and is
   noted as a candidate for a future pass rather than tackled here.)

## What Phase 2 deliberately did not touch (carried forward, not forgotten)

- **`Home/Setup.cshtml`'s setup-wizard step nav** (desktop buttons vs. mobile `<select>`, noted
  above) — smaller, different mechanism than the tab-bar triplication, lower priority.
- **Adopting `.status-badge`/`.ct-empty-state` into any actual screen** — these are defined and
  ready, but rolling them into e.g. reminder urgency or plan priority badges touches `.cshtml`
  files and should happen alongside the relevant feature phase (3, 6) rather than blind here.
- **Typography scale** — the existing `html { font-size: 14px / 16px }` responsive base and
  Bootstrap's own type scale weren't touched; no evidence surfaced that they need to change.
- **`prefers-reduced-motion` support** — noted in `UI_SPEC.md` as a Phase 14 accessibility item.

## Result

Both increments complete and user-verified in a live browser. The specific structural issue Phase 0
flagged (triplicated shell/navigation markup) is now fully resolved — confirmed by auditing every
`@section Nav`-bearing view in the codebase, not just the two that were fixed. Remaining Phase 2
scope (typography, forms/tables/dialogs styling, badge/empty-state adoption per screen) is smaller,
lower-risk, CSS-only-or-additive work better done incrementally alongside the feature phases that
touch each screen, per `CLAUDE.md`'s "preserve working functionality... replace incrementally"
principle — see `docs/execution/STATE.md` for the open question on whether to continue here or
move to Phase 3.
