# UI_SPEC.md

Target UX/design system and interaction rules for Car Tracker, established in Phase 2. This is the
reference for all future screen work (Phase 2 foundation now; Phases 3+ roll it out screen-by-screen
as each area gets touched for feature work).

## Constraints this spec works within

- Built on **Bootstrap 5.3.2** (already in use) — extend it with tokens and overrides, don't
  replace it. Bootstrap already owns dark/light theme switching via `data-bs-theme` on `<html>`;
  every new rule must respect that mechanism, never fight it with a separate theme system.
  A working custom-theme-upload feature also already exists (`ThemeController`, layered as an
  extra stylesheet after `site.css`) — new tokens must not break a user's uploaded custom theme by
  being un-overridable.
- No SPA framework, no CSS build step/preprocessor — plain CSS in `wwwroot/css/site.css`, loaded
  after Bootstrap. Keep it that way; introducing Sass/PostCSS is a dependency change and would need
  its own justification.
- ~200 Razor views currently hand-roll their own markup against Bootstrap utility classes directly.
  The design system's job is to give them a consistent, named vocabulary (tokens + component
  classes) to converge on over time — not to rewrite them all at once.

## Design tokens

CSS custom properties defined on `:root`, layered on top of Bootstrap's own `--bs-*` variables
(reference them, don't duplicate their values — Bootstrap's variables already flip correctly with
`data-bs-theme`, so anything token-based that needs to be theme-aware should point at a `--bs-*`
variable rather than hardcoding a color).

- **Spacing scale**: `--ct-space-1` through `--ct-space-6` (0.25rem step, matching Bootstrap's own
  spacer scale so the two systems stay mentally interchangeable).
- **Radius scale**: `--ct-radius-sm` / `--ct-radius-md` / `--ct-radius-lg` — consolidates the
  several different hardcoded `border-radius` values already scattered across `site.css` (4px on
  `.card`/`.taskCard`, `0.375rem` on uploaders/inspection values, `0.438rem` on skinny accordions).
- **Elevation scale**: `--ct-shadow-sm` / `--ct-shadow-md` — consolidates the two near-identical
  box-shadow strings duplicated across `.card`, `.taskCard`, `.kiosk-card:hover` today.
- **Motion**: `--ct-transition-fast` / `--ct-transition-base` — named versions of the `.3s`/`.15s`
  durations already used ad hoc throughout.

Colors are **not** tokenized separately — Bootstrap's semantic color variables (`--bs-primary`,
`--bs-body-bg`, `--bs-secondary-bg-subtle`, `--bs-danger-bg-subtle`, etc.) already exist, are
already used correctly in most of the codebase, and already handle dark/light. The one rule going
forward: never hardcode a hex/rgb color for anything that appears in both themes — reference a
`--bs-*` variable instead. (See "Fixed in Phase 2" below for the loading-overlay violation of this
rule that already existed.)

## Components

- **Cards**: consistent radius/elevation/hover-transform via the new tokens (`.card`, `.taskCard`,
  `.kiosk-card` already share near-identical intent — they now share the same token values instead
  of independently hardcoded numbers).
- **Buttons, forms**: use Bootstrap defaults; `.btn-adaptive` (a theme-aware neutral button variant)
  stays as the one deliberate override, already correctly theme-aware.
- **Tables**: `.ll-responsive-table` (card-per-row on mobile) is the existing pattern for
  record-list tables — keep it as the standard for any new tabular data (Parts, Government Data)
  rather than introducing a second table pattern.
- **Dialogs**: Bootstrap modals + SweetAlert2 for confirms/prompts/toasts — keep this split as-is
  (SweetAlert2 for lightweight interactions, modals for real forms).
- **Status badges / semantic states**: new primitive classes `.status-badge`,
  `.status-badge-success` / `-warning` / `-danger` / `-neutral`, mapped to Bootstrap's
  `-bg-subtle`/`-text-emphasis` pairs so they're theme-correct automatically. Intended target
  usage going forward: reminder urgency (`ReminderUrgency`: NotUrgent/Urgent/VeryUrgent/PastDue),
  planned-work priority (`PlanPriority`), and future government-data status (MOT pass/fail/due).
  Not yet adopted into any view in this pass (see Phase 2 task scope below) — available for Phase 3+
  to use as each screen is touched.
- **Loading state**: `.sloader`/`.loader` (existing spinner overlay) — fixed in this phase to be
  theme-aware (see below); this remains the standard loading treatment.
- **Empty state**: new `.ct-empty-state` primitive (icon + message + optional action), for use
  wherever a list/table currently just renders nothing when there's no data. Not yet adopted into
  any view in this pass.
- **Error state**: existing `401.cshtml`/`Error.cshtml` pages stay as the page-level pattern;
  inline/partial error messaging already goes through `OperationResponse`-driven SweetAlert2 toasts
  (`shared.js`) — no change needed to that mechanism.

## Navigation / shell

**Not restructured in this pass** — see "What Phase 2 deliberately did not touch" below. The
existing shell (`_Layout.cshtml` + per-page `@section Nav` blocks) is visually refreshed via the
token system (spacing/radius/shadow now consistent) without changing its markup structure. A real
consolidation of the triplicated tab-bar markup (desktop / "more" dropdown / mobile off-canvas,
found duplicated in `Home/Index.cshtml`, `Vehicle/Index.cshtml`, and similarly structured in
`Kiosk/Index.cshtml`, `Migration/Index.cshtml`, `Home/Setup.cshtml`, `Admin/Index.cshtml`,
`Views/API/Index.cshtml`) is flagged as follow-up work that needs interactive/visual verification
before attempting — see `docs/execution/PHASE_02.md`.

## Responsive rules

Keep the existing breakpoint strategy (`site.css` media queries at 575/576/768/992/1200/1400px,
off-canvas mobile nav below 576px) — it works and a wholesale responsive-system replacement isn't
justified by anything in `REQUIREMENTS.md`. New components should use the same breakpoints rather
than introducing new ones.

## Interaction rules

- Respect the existing `data-bs-theme` mechanism and the user's custom-theme-upload feature —
  never hardcode a color that would look wrong under a theme the design system's author didn't
  anticipate.
- Respect `prefers-reduced-motion` is **not currently handled anywhere** in the codebase (several
  CSS animations: card hover-scale, bell-shake, table-row-shake, mobile-nav slide-in). Flagged as a
  future accessibility follow-up (Phase 14 — Accessibility), not fixed in this pass since it's
  additive/independent of the token work and out of this task's scope.
- New primitive classes (`.status-badge-*`, `.ct-empty-state`) are opt-in — existing views keep
  working unchanged until a later phase deliberately adopts them while touching that screen for
  feature work, per `CLAUDE.md`'s "preserve working functionality until replacement has parity."
