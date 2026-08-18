# UI_SPEC.md

Target UX/design system and interaction rules for Car Tracker. This is the reference for all future
screen work. As of the Zara + Magneto UI overhaul, the system described here is the app's **real,
permanent default** — it lives directly in `wwwroot/css/site.css`, not in an opt-in theme. Full
increment-by-increment implementation history, research provenance, and known gaps live in
`docs/execution/UI_TRANSITION.md` — read that for the "why" behind any specific rule below.

## Constraints this spec works within

- Built on **Bootstrap 5.3.2** (already in use) — extend it with tokens and overrides, don't
  replace it. Bootstrap already owns dark/light theme switching via `data-bs-theme` on `<html>`;
  every rule here respects that mechanism rather than fighting it with a separate theme system.
  The custom-theme-upload feature (`ThemeController`, `/css/theme.css`, layered after `site.css`)
  still exists and still works — but it is no longer where this design system lives. Its role going
  forward is what it originally was: an *optional alternate* theme a user can upload, not a staging
  area for the app's own default look.
- No SPA framework, no CSS build step/preprocessor — plain CSS in `wwwroot/css/site.css`, loaded
  directly, no Sass/PostCSS compilation step. Keep it that way.
- Two self-hosted variable fonts under `wwwroot/lib/fonts/` (Fraunces — display/headings, 4 axes:
  wght, opsz, SOFT, WONK; Work Sans — body/UI), both OFL-licensed, no CDN dependency.
- Self-hosted Phosphor Icons (`wwwroot/lib/phosphor/`) replace Bootstrap Icons app-wide via a CSS-only
  `@font-face` + codepoint-override layer — no Razor/JS changes, no CDN dependency.
- Self-hosted Flatpickr replaces bootstrap-datepicker for all date inputs, including the inline
  reminder calendar.

## Design language, in one paragraph

Two references, deliberately combined rather than either alone: **Zara** supplies restraint —
monochrome-first structure, flat/sharp corners, oversized photography, metadata that reveals on hover
rather than sitting on the page permanently. **Magneto** (a print car-culture quarterly, studied from
the actual 220-page Spring 2024 issue, not secondary sources) supplies what restraint alone can't —
genuine typographic character at a short, deliberately finite list of named moments, colour used as a
considered device rather than only a narrow status utility, and layouts that follow what the content
actually is rather than forcing every screen through one identical template. The operating rule that
keeps these from fighting each other: **restraint governs structure everywhere; voice is allowed only
at the named moments listed below.** That list is closed — a future change that wants to add a new
"named moment" should update this section explicitly, not creep in ad hoc.

**Named hero-voice moments** (Fraunces at its full hero axis settings — heavy weight, extreme optical
size, WONK on): the app wordmark ("Car Tracker" in the nav, replacing the old LubeLogger logo image),
the Garage masthead vehicle-count numeral, a vehicle's own title, and a vehicle Dashboard's headline
stat numbers. Everywhere else uses Fraunces at ordinary heading weight (section headers, card titles)
or Work Sans (body copy, forms, tables, buttons) — not the hero treatment.

## Design tokens

CSS custom properties defined on `:root` in `wwwroot/css/site.css`, redefined under
`[data-bs-theme="dark"]` for dark mode. Reference `--bs-*` variables rather than duplicating raw
values wherever Bootstrap already owns the concept, so anything token-based stays theme-aware
automatically.

- **Spacing**: `--ct-space-1` through `--ct-space-7` (0.25rem step; `--ct-space-7` — 3rem — is a
  component-internal cap above Bootstrap's own spacer scale, used sparingly for hero-moment breathing
  room).
- **Radius**: `--ct-radius-sm` / `--ct-radius-md` / `--ct-radius-lg` (Bootstrap-derived) plus
  `--ct-radius-card`, deliberately set to `0` — the app-wide flat/sharp-corner language is Zara's
  single most load-bearing structural trait and applies to cards, buttons, inputs, and dropdowns.
- **Elevation**: `--ct-shadow-sm` (`none`) / `--ct-shadow-md` — shadows are used sparingly, not as
  the default card treatment; flat surfaces plus a hairline border do most of the separation work.
- **Motion**: `--ct-transition-fast` / `--ct-transition-base`, both neutralized under
  `prefers-reduced-motion: reduce` — see Motion below.
- **Heading/body font**: `--ct-heading-font-family` (Fraunces) / `--bs-body-font-family` (Work Sans).
- **Focus ring**: `--ct-focus-ring-inner` / `--ct-focus-ring-outer`, theme-aware.

**Colours ARE tokenized** — this was true only partially before the overhaul and is fully true now.
Bootstrap's own semantic variables (`--bs-primary`, `--bs-success`, `--bs-info`, `--bs-warning`,
`--bs-danger`, and their `-bg-subtle`/`-border-subtle`/`-text-emphasis` pairs) are all retuned in
`site.css` to the app's actual palette — ink/paper monochrome base in light mode, inverted in dark —
rather than left at Bootstrap's stock blue/green/red. One additional, deliberately separate role:
`--ct-spotlight-bg` / `--ct-spotlight-color` / `--ct-spotlight-solid` / `--ct-spotlight-solid-color`
— a narrow warm-accent register reserved for "needs your attention" moments (reminder badges, the
poster-style empty Garage state), never reused for routine buttons or merged with `--bs-primary`/
`--bs-danger`. Two hard rules going forward:
1. Never hardcode a hex/rgb colour anywhere that appears in both themes — reference a `--bs-*` or
   `--ct-*` variable instead.
2. **A CSS custom-property override is not sufficient proof a component is themed correctly.**
   Bootstrap 5.3's own `.btn-*`/`.btn-outline-*`, `.form-check-input:checked`, `.form-range` thumb,
   `.dropdown-menu` active-item, and `.nav-pills` active state all hardcode their *own* component-
   local `--bs-btn-bg`-style variables at Bootstrap's build time — they do **not** derive from the
   root `--bs-primary`/etc. tokens by default. This class of bug is invisible to build/curl
   verification (the HTML class name is correct regardless of rendered colour) and was only caught by
   an actual browser screenshot after 11 increments. `site.css` now explicitly re-points every one of
   these component-local variables at the root tokens — if a new Bootstrap component is themed in the
   future, check its compiled CSS for this pattern before assuming a root-level override is enough.

## Components

- **Cards / surfaces**: flat (`--ct-radius-card: 0`), hairline border, minimal/no shadow — see
  tokens above.
- **Buttons**: Zara's actual button voice — confident, uppercase, letter-tracked. Hover/active shades
  use `color-mix(in srgb, var(--bs-X) 85%, var(--bs-body-color) 15%)`, which is correct in both light
  and dark mode from one rule (mixing in body-color darkens in light mode, lightens in dark mode,
  since body-color itself flips between ink and paper).
- **Forms**: Work Sans, Bootstrap structure, themed via the token re-pointing described above.
  Flatpickr (not bootstrap-datepicker) for every date input.
- **Icons**: Phosphor (not Bootstrap Icons) app-wide, via CSS override — see Constraints above.
- **Tables**: `.ll-responsive-table` (card-per-row on mobile) remains the standard pattern for
  record-list tables (Parts, Odometer, Government Data) — deliberately kept tabular where data is
  genuinely comparison-dense, per the "content dictates form" principle below.
- **Documents**: rebuilt as a considered grid (thumbnail, bold filename, record-type + date excerpt)
  rather than a bare filename/icon table — the concrete case where Magneto's article-index anatomy
  mapped directly onto an existing screen.
- **Status badges**: `.status-badge`, `.status-badge-success`/`-warning`/`-danger`/`-neutral`, mapped
  to the now-fully-tokenized `-bg-subtle`/`-text-emphasis` pairs. Used for reminder urgency, plan
  priority, government-data status.
- **Empty states**: `.ct-empty-state` primitive, large Fraunces headline + short anchoring rule
  (Magneto's pull-quote device, translated), used by Documents/Parts/Government Data. The fully-empty
  Garage (zero vehicles at all) gets its own poster-style treatment — solid `--ct-spotlight-solid`
  background, confident type, no invented copy (reuses the existing "Add New Vehicle" translation key
  rather than a new untranslated string).
- **Countdown/index pattern**: a large rank/count numeral as the dominant element with a small,
  uniform-treatment thumbnail as support — used for the Garage masthead (vehicle count) and each
  Garage card's index numeral. Scoped deliberately to comparison/index contexts; not the same pattern
  as a single feature's own varied-photo-crop treatment (which this app doesn't currently have a
  context for).
- **Chapter-divider motif**: full-bleed solid colour + bold statement, reused only where a genuine
  "chapter closed" moment exists — currently just a sold vehicle's Dashboard hero (`.report-hero-
  sold-band`). Deliberately not forced onto other screens; see `UI_TRANSITION.md`'s Phase 5 for the
  reasoning against over-applying it.
- **Pull quotes**: large bold Fraunces + short rule beneath, used by the empty-state primitive above.
- **Hover/interactive language**: icon-only actions (`.link-body-emphasis` — vehicle-edit icon,
  attachment-preview controls) get an explicit colour-only hover/focus-visible transition to
  `--bs-primary`. Bootstrap's own default here has zero feedback state — a real, verified gap, not
  invented busywork.
- **Loading state**: `.sloader`/`.loader`, theme-aware.
- **Error state**: existing `401.cshtml`/`Error.cshtml` pages; inline errors via `OperationResponse`-
  driven SweetAlert2 toasts (`shared.js`) — unchanged.

## Navigation / shell

- **Simple title-bar pages** (Admin, API, Setup, Migration): unified into one shared partial,
  `Views/Shared/_SimpleNavBar.cshtml`. These four views only ever had a logo + static title, no tab
  strip — genuinely safe to de-duplicate.
- **Tab-strip pages** (`Home/Index`, `Vehicle/Index`): deliberately **not** unified into the same
  partial — investigation found they share a structurally different pattern (`TabOrder`-driven tabs,
  reminder-bell markup, a user/admin dropdown, a duplicated mobile nav) with enough real per-view
  logic that forcing one partial would mean an unreadable parameter surface. This is a stated scope
  boundary, not an oversight — re-evaluate only if the duplication becomes a genuine maintenance
  cost, not just for its own sake.
- **Wordmark**: both nav patterns now render a typographic "Car Tracker" wordmark (Fraunces, hero
  axis settings) in place of the old `lubelogger_logo*.png` default — the single biggest literal
  "looks like LubeLogger" signal, since it was an actual branded image file, not a CSS/theme issue.
  A real admin-uploaded custom logo still takes priority over the wordmark (compared against
  `StaticHelper.DefaultLogoPath`/`DefaultSmallLogoPath`), and a vehicle's own thumbnail still takes
  priority on `Vehicle/Index` when `ShowVehicleThumbnail` is on — the wordmark is only ever a
  fallback, never forced over real user content.
- **Active-tab indicator**: an underline in `--bs-primary`, replacing what used to be no visual
  active/inactive distinction at all beyond default text colour.
- **`checkNavBarOverflow()`'s hardcoded `48px` row-height threshold** (`wwwroot/js/shared.js`) must
  stay valid for any future nav change — the wordmark wrapper carries an explicit `min-height:48px`
  specifically to preserve this without needing to re-derive the constant. If a future change alters
  effective nav row height, re-check this threshold as part of that change, not as an afterthought.

## Responsive rules

Existing breakpoint strategy unchanged (575/576/768/992/1200/1400px, off-canvas mobile nav below
576px) — new components use the same breakpoints rather than introducing new ones.

## Interaction rules

- Respect `data-bs-theme` and the (now secondary-role) custom-theme-upload feature — never hardcode
  a colour that would look wrong under an uploaded alternate theme.
- **`prefers-reduced-motion: reduce` is handled globally** — neutralizes `--ct-transition-base` and
  every transform/opacity hover treatment (card lift, metrics hover-reveal). This was a real,
  previously-unaddressed accessibility gap, now closed.
- One vanilla-JS `IntersectionObserver` reveal (no animation library — this is a local, single-user
  app; a full library is unjustified dependency weight) is used for exactly two named moments: the
  Garage masthead and a vehicle's own title. Not applied more broadly — same finite-named-moments
  discipline as the typography rule above.
- New primitive classes (`.status-badge-*`, `.ct-empty-state`) are opt-in for screens not yet touched,
  but are now the actual default for the screens listed under Components above — don't reintroduce
  the pre-overhaul patterns they replaced.

## What's still open

See `docs/execution/UI_TRANSITION.md`'s "Known open items" for the current, maintained list
(currently: `bootstrap-tagsinput` still unthemed; the optional Phase 2 chrome-accent moment never
added; a few empty/sold-state renders verified by construction rather than live browser check due to
this dev environment's test data). Treat that file, not this one, as the source of truth for
open/in-progress items — this file describes the target system as designed, not day-to-day status.
