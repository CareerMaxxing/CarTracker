# PHASE_18 — Post-Deployment Functionality Review

New phase, started after the user raised a serious concern following Phase 17's deployment: a brand-new
feature (MOT Ignore) appeared completely non-functional in their hands with no error shown, and they
asked directly whether things being shipped were actually working - "go through each workflow by doing
them yourself and see if its effective... go through the codebase and find whether or not things that
have been produced are actually functional and useful or not." They also raised three specific,
concrete complaints in the same message: the Kanban board's headers aren't readable and everything
renders too small for a "long term" tool; Parts and Supplies seem redundant; and the Calendar's
timeline looks empty despite real vehicle history existing, with a suggestion that a timeline belongs
on the vehicle page instead.

This phase's discipline is different from a normal feature phase: every finding below was verified
against this household's real data (BMW Z4 id=1, Volvo S80 id=2) via curl before being reported, not
assumed or guessed at from reading code alone - matching the user's explicit ask to actually exercise
things, not just claim they work.

## Root cause found: static assets were never being cache-busted

Investigated the reported "pressing Ignore does nothing, no error" first, since it was the most acute
and most recent claim. Confirmed the server-side action worked perfectly via curl (identical to how
every other MOT action had been verified all session) - the bug was entirely client-side.
`StaticHelper.VersionNumber`, used as the `?v=` cache-busting query string on every `<script>`/`<link>`
tag app-wide, is a hardcoded upstream release constant that stayed `"1.7.0"` through this fork's entire
Phase 15-17 session - every JS file update since Phase 15 was served under the byte-identical URL, so a
browser that had cached an earlier copy had no signal to ever re-fetch. A brand-new function
(`ignoreMotAdvisory`, added same-day) called from a stale cached page throws a silent
`ReferenceError` - exactly matching "does nothing, no confirmation, no error."

Fixed with `StaticHelper.AssetCacheBustStamp`, derived from the deployed binary's own last-write time
so it changes automatically on every future publish - nothing to remember, can't silently regress
again. `VersionNumber` itself was left untouched for its actual purpose (health/info API endpoints,
the Settings page's displayed version). All 29 cache-busting call sites across 12 view files switched
over. Full detail/verification/deployment already recorded in `STATE.md`'s "Do not" list and commit
`3005cf2` - not duplicated here since it's genuinely app-wide, not MOT-specific.

**This is very likely a partial explanation for the broader "the app has lost its great usage"
feeling** - any JS-dependent feature shipped or changed this entire session could have been silently
serving stale behavior on the user's actual device, invisible to my own curl-based verification (curl
never caches, so every one of my own checks always saw the fresh code) and invisible to server logs
(the browser never even sent the request for features that failed at the "function doesn't exist"
stage). This doesn't mean everything WAS broken - the user was actively using and confirming several
features worked during this session - but it means my verification methodology had a real blind spot
that's now closed.

## Increment 1: Kanban board - unreadable/undersized headers

### Investigation

Read the actual CSS (not assumed): `.swimlane` had no minimum width, `.swimlane-container` had no
`flex-wrap` and no horizontal scroll, so the 6 (now 7, with Ignored) `col-2 flex-grow-1` lanes force-
shrink to fit whatever viewport width is available with nothing to prevent it. The swimlane header text
(`<span class="display-7 text-truncate">`) uses `.display-7` - a page-heading-scale class
(`font-size: calc(1.325rem + 0.9vw)`, ~24-32px) - inside a `text-truncate` wrapper, so at any width
narrower than a full desktop monitor, multi-word labels like "Parts Sourced" and "In Progress" get
ellipsis-truncated. On mobile specifically (`max-width: 575px`), `.swimlane-header` is set to
`display: none` entirely - headers don't render at all there (the top mobile-nav button row, which
shows one lane at a time with its own label+count, substitutes for it - a different, not obviously
broken, interaction pattern left alone here).

### Fix

- `.swimlane` given a real `min-width: 11rem` - a column never has to fit into less space than a label
  like "Parts Sourced" actually needs.
- `.swimlane-container` given `overflow-x: auto` + `flex-wrap: nowrap` - once lanes hit their minimum
  width, the board scrolls sideways for more rather than crushing every lane further, the standard
  Kanban-board pattern (Trello etc.), legible at any viewport width instead of being unreadable below
  some untested threshold.
- `.swimlane-header .display-7` scoped down to `font-size: 1rem; font-weight: 600` - sized for a column
  label sharing its 5vh header row with a count badge, not a page heading. Scoped to this context only
  (not touching `.display-7` itself, used elsewhere for genuine headings).

### Verification

`dotnet build`/`dotnet test` - 0 new warnings/errors, 30/30 passing (CSS-only change, no C#/Razor
touched). Dev instance: confirmed both edited CSS rules serve correctly with no syntax breakage via
curl against the raw stylesheet. **Explicitly not claimed as visually verified** - there is no browser
tool in this environment, and this fix is grounded in reading the actual CSS values and Bootstrap grid
math, not in seeing the result render. The user needs to look at it live to confirm it actually reads
better, same limitation that has applied to every visual change this whole project.

## Findings (not yet actioned - need the user's direction)

### Parts vs Supplies: a real, confirmed, unfinished migration

Not a misunderstanding on the user's part - the codebase's own docs say so directly.
`docs/DATA_MODEL.md`: *"SupplyRecord conflates 'the part' with 'a specific purchase lot of that
part'"*. Phase 5 (`docs/execution/PHASE_05.md`) added Part/PartPurchase as a cleaner catalog-vs-
purchase split explicitly intended to eventually replace SupplyRecord, built additively "rather than
modifying SupplyRecord... no regression risk" - but the migration was deliberately left unfinished:
`PartPurchase.RequisitionHistory` exists on the model but is never wired to anything (confirmed dead),
Parts have no CSV import/export or ExtraFields unlike Supplies, and only Supplies actually connects to
the Planner's requisition/consumption workflow. Both live as separate tabs on the same vehicle page,
asking for nearly-identical fields (part number, description, supplier, quantity, cost, date) - a
real, confirmed source of "which one do I use" confusion, not a false impression.

**This needs the user's explicit direction before any code changes** - per `CLAUDE.md`'s stop
conditions, a decision here likely means either a real data migration (Supplies → Parts, retiring
the older model) or a deliberate scope decision to finish wiring Parts into consumption and leave
Supplies as a legacy/import-only path - both are real architectural choices, not something to guess at
unilaterally the way normal task-level implementation choices are.

### Calendar vs Timeline: two different features, one working as designed, one with a real narrower gap

Confirmed against the real household data: **the Calendar tab (`/Home/Calendar`) is legitimately
empty** - it queries only date-based `ReminderRecord`s (explicitly excluding mileage-based ones), and
this household has zero reminders configured on either real vehicle (`GET /api/vehicle/reminders`
returns `[]` for both). It is a "what's coming up" view, not a "what's happened" view - the user's
expectation that it should show real vehicle history doesn't match what this tab was ever built to do,
and there's no bug in its query logic.

**The user's own suggested fix already exists**: a genuine chronological timeline
(`_VehicleHistory.cshtml`, fed by `ReportController.GetVehicleHistory`) already lives on the vehicle
page, not the Calendar - confirmed via curl it renders real content (6KB, not an empty state) for the
BMW Z4. The user may simply not have found it, or be conflating it with the empty Calendar tab. It
does have one real, confirmed gap worth flagging: it aggregates ServiceRecord/CollisionRecord/
UpgradeRecord/TaxRecord but never PlanRecord - so none of this session's new MOT-Planner work
(including anything marked Resolved) will ever appear there, even though a resolved MOT advisory
represents something genuinely dealt with. Whether that's worth changing depends on whether "history"
should include planned/resolved work that never became a formal ServiceRecord - a smaller, more
contained decision than the Parts/Supplies one, but still worth confirming with the user rather than
assuming.
