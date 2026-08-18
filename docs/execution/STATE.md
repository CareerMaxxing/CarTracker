# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 16 — Sidebar App Shell & Dashboard Redesign (Increment 9 complete,
                     Increment 10 next)
Current task:       PHASE-16-09 (see docs/execution/PHASE_16.md) — Planned Maintenance list added to
                     the Dashboard's widget row. Both states verified, including the specific
                     urgency/badge mapping against a throwaway past-due reminder.
Status:             Full plan/decision history in git history + PHASE_16.md's intro, not re-summarized
                     here (increments 1-8's full detail is fully preserved there). Increments 1-8
                     committed (b669299, b50f3e1, b9ab158, 4a4df63, 72ba466, dda1d24, 0cc6271, 7d71eec,
                     ac74289). Increment 9: found the full per-reminder list was already computed in
                     ReportController.GetReportPartialView for the pie chart's aggregate counts, then
                     discarded - added ReportViewModel.UpcomingReminders + one controller line
                     (`reminders.OrderByDescending(x=>x.Urgency).ThenBy(x=>x.DueDays).Take(5)`) to keep
                     it instead of a new query. Built Report/_PlannedMaintenanceWidget.cshtml as a
                     list variant of the same .report-widget-card wrapper (confirmed its flex-column
                     layout works for a list, not just a single stat - no new card shape needed).
                     Caught a real small mistake before shipping: translating reminder.Urgency.
                     ToString() (raw enum name, "PastDue" with no space) would have produced an ugly
                     label - matched this app's own existing translation-key convention
                     ("Past Due"/"Very Urgent" etc., found already in _ReminderRecords.cshtml/
                     _Kiosk.cshtml) instead. First real adoption of .status-badge-spotlight for the
                     exact case its own introducing CSS comment named ("an overdue reminder on a
                     Garage card") but had never been wired to a caller. While checking for stray
                     .status-badge matches to confirm the empty state, found 4 UNRELATED pre-existing
                     uses on the same page (the Government Data panel, Phase 8) - correctly attributed
                     to that, not confused with this increment; the "not yet adopted by any view" CSS
                     comment is simply stale documentation predating Phase 8, not touched (out of
                     scope). Verified both states for real: both vehicles show "No Data" (neither has
                     reminders yet); a throwaway past-due reminder added to vehicle 1 via
                     /api/vehicle/reminders/add rendered with exactly the "Past Due" label and
                     .status-badge-spotlight class - then deleted, empty state reconfirmed restored.
                     Verified: dotnet build (0 errors), dotnet test (10/10), both vehicles' pages still
                     load, /css/site.css still balanced (441/441). Not yet committed - pending
                     alongside this STATE.md update.
Last completed:      Phase 15 (Remote Access & Persistent Hosting) - all 5 increments resolved (4
                     explicitly declined by the user, not skipped). See docs/execution/PHASE_15.md.
                     Phase 14's remaining areas (icon-button labels, keyboard nav, form labels, alt
                     text, mobile/responsive validation, performance) are still open, not abandoned -
                     both Phase 15 and now Phase 16 were started because the user raised new,
                     higher-priority requests, not because Phase 14 finished.
Next task:           Increment 10 (Recent Activity feed - extend the existing GetVehicleHistory
                     projection, currently Service/Repair/Upgrade/Tax only per ARCHITECTURE.md's
                     research, to also include GasRecord, sorted/truncated to latest N - this is a
                     genuinely NEW controller method or a modified existing one, not pure reuse like
                     7-9, since GetVehicleHistory as it exists is a full filterable history report,
                     not a "latest N" feed shape - read that method fully before assuming how much can
                     be reused vs. needs a new thin wrapper), then Increment 11 (Magneto-motif promo
                     tile, purely cosmetic, last). See the approved plan / PHASE_16.md's intro for the
                     full per-increment breakdown. Do not re-litigate the landing-page decision
                     (Increment 4) - answered directly by the user. Real vehicle data: vehicleId=1
                     (BMW Z4 - has cost history, a throwaway reminder was tested and cleaned up, no
                     MPG history), vehicleId=2 (Volvo S80 - no cost/MPG/reminder history yet) - vehicle
                     1 likely has enough Service/Tax/etc. history for Increment 10's populated state
                     (it already showed a real "Expenses by Type" pie chart with Repairs/Service data
                     in the screenshot the user shared), but confirm this directly rather than assume.
                     Before wiring any new "add record" trigger, learn from Increment 6's bug: verify
                     empirically where the target modal's shell actually lives. If a throwaway test
                     vehicle/record is ever needed again, always delete it before finishing (see
                     Increment 5/7/9's pattern).
Known blockers:      1. No browser/screenshot tool in this environment - Phase 14's remaining
                        accessibility work would still need static-code-audit + curl verification,
                        not live screen-reader/keyboard testing.
                     2. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items, including Phase 14's un-fixed accessibility
                        findings (icon buttons, keyboard nav, form labels, alt text, 2 unresolved
                        modals) with concrete file pointers so a future increment can start without
                        re-auditing.
Open decisions:      Whether to fix the two flagged-but-not-yet-fixed security gaps (no login
                     rate-limiting/lockout, unsalted SHA-256 password hashing) before or after Phase
                     15 Increment 3 turns on auth - current plan is to defer both (Tailscale's
                     device-authorization layer already means reaching the login form requires
                     control of a device already inside the tailnet) and record them in DEFERRED.md,
                     but this hasn't been done yet since Increment 3 hasn't started. What to
                     prioritize after Phase 15 (return to Phase 14's open areas, or something else) -
                     ask the user rather than assume.
Do not:              Assume Phase 14 is "done" - three increments complete (security/tests/modal
                     accessibility), several areas still open. Do not re-introduce a duplicate title
                     id when adding a new modal - grep for the exact id string across Views/ first;
                     this bit twice already (_AccountModal.cshtml/_AttachmentPreview.cshtml, and
                     tabReorderModal/translationEditorModal both being obvious copy-paste artifacts
                     nobody had caught before). When adding a NEW AJAX-loaded modal, follow the
                     established pattern: outer shell `<div class="modal fade" id="XModal"
                     aria-labelledby="XModalLabel">` wrapping an empty `<div class="modal-content"
                     id="XModalContent">`, content partial's `<h5 class="modal-title" id="XModalLabel">`
                     - don't invent a new structural pattern. Do not guess a modal's content-providing
                     partial from filename similarity alone - trace the real JS
                     `$.get(url).html(data)` -> controller action -> `PartialView(...)` chain, several
                     pairings in this codebase aren't obvious from names. Do not re-add the old
                     OnPrepareResponse-based auth checks on the static file routes. Do not implement
                     CSRF tokens or a CSP header without discussing first. Do not assume SQLite is
                     available anywhere in this codebase. Do not assume a fresh vehicle/user has any
                     tabs visible beyond Dashboard - VisibleTabs defaults to [Dashboard] only. Do not
                     add a "MOT"/"Part"/etc. (any non-record-type) value to the ImportMode enum. When
                     any controller does a "move files from temp"/reconstruct-UploadedFiles step, it
                     MUST explicitly copy every field it wants to keep. When adding a new entity type
                     with its own Files/attachments, wire it into GetVehicleDocuments/
                     DeleteVehicleRecords/ClearUnlinkedDocuments in Logic/VehicleLogic.cs. Any enum
                     embedded directly in a type used as a JSON request-body wire format needs its
                     own JsonStringEnumConverter. When calling record-add API endpoints for testing,
                     field names/casing are inconsistent across export models and dates must match
                     the server's locale (dd/mm/yyyy here). Note records require both Description AND
                     NoteText. PlanRecord's Priority must be Critical/Normal/Low. Some fields (e.g.
                     Vehicle.HasOdometerAdjustment) are MVC-only, not exposed on the API's
                     *ImportModel DTOs. Part is NOT vehicle-scoped (global catalog) but PartPurchase
                     IS (VehicleId, 0=shop-wide). PartPurchase.QuantityRemaining must be set
                     explicitly by the caller, never by ToPartPurchase(). PlanRecord.ActualCost is
                     preferred over Cost (estimate) by the completion-conversion logic when non-zero.
                     Government data is looked up by Vehicle.LicensePlate, never VehicleIdentifier.
                     OdometerRecord.Source must be preserved (not reset to Manual) on manual edits of
                     auto-inserted records. The root/dev user's config (EnableAuth=false) reads
                     directly from data/config/userConfig.json but is cached in-memory for up to 1
                     hour - restart the app after editing it. Tests: dotnet test
                     Tests/CarCareTracker.Tests.csproj from the repo root, fully isolated, safe
                     anytime. Every data/ path in this app (StaticHelper.DbName/UserConfigPath/
                     ServerConfigPath) is CWD-relative, not ContentRootPath-relative - Program.cs now
                     has a WindowsServiceHelpers.IsWindowsService()-gated
                     Directory.SetCurrentDirectory(AppContext.BaseDirectory) fix for the Windows
                     Service case specifically; do not remove or broaden that gate, and do not assume
                     it also covers Docker or other deployment modes without checking their CWD
                     behavior too. Never flip EnableAuth:true by hand-editing userConfig.json without
                     credentials already set via the Settings "Enable Authentication" UI flow first
                     (POST /Login/CreateLoginCreds) - it will lock everyone out; the UI flow sets
                     EnableAuth and the credential hashes atomically. If ever locked out: stop the
                     process/service, hand-edit EnableAuth:false back, restart. Do not re-raise
                     "should we enable auth" unprompted - the user explicitly declined it in Phase 15
                     (device-level protection + Tailscale's own device authorization judged
                     sufficient); if they raise it themselves later, the UI steps are already
                     documented in PHASE_15.md's Increment 4. The Windows Service at
                     C:\Services\CarTracker is now the live/production copy of the app - do not treat
                     the dev repo's data/ folder as authoritative or assume it reflects current real
                     vehicle data; it's a frozen fallback copy from the moment Phase 15 Increment 2
                     ran. dotnet run from the dev repo still works for development but now operates on
                     a separate, diverging dataset - be explicit with the user about which copy
                     they're looking at if this ever comes up.
Last validation:     Phase 15 + Phase 16 Increments 1-8's full validation history lives in
                     docs/execution/PHASE_15.md / PHASE_16.md. Phase 16 Increment 9 (current): dotnet
                     build (0 errors); dotnet test (10/10 passing); dev instance on port 5300; both
                     vehicles confirmed showing "No Data" (neither has reminders); a throwaway
                     past-due reminder on vehicle 1 (via /api/vehicle/reminders/add) confirmed
                     rendering with exactly the "Past Due" label + status-badge-spotlight class - the
                     specific urgency/badge mapping verified, not just "a row appeared" - then
                     deleted via /api/vehicle/reminders/delete, empty state reconfirmed restored;
                     both vehicles' Vehicle/Index pages still 200; /css/site.css balanced (441/441) —
                     2026-08-18.
Last commit:         ac74289 — "Phase 16 Increment 8: Total Spent widget" — 2026-08-18. Increment 9
                     (this entry) not yet committed - pending alongside this STATE.md update.
```

## Completed initiative: Zara + Magneto UI overhaul (separate from the roadmap above)

**Status: finalized and promoted to the app's permanent default.** User signalled completion
("we are finalising on this, wrap it up") and the whole design system now lives directly in the
tracked `wwwroot/css/site.css` — there is no longer a reversible theme layer to be aware of. Read
`docs/execution/UI_TRANSITION.md` for the full increment-by-increment log (11 increments + 8 further
phases + Magneto-PDF-grounded phases + the final promotion entry) and `docs/UI_SPEC.md` for the
design system as designed. Critical facts a fresh session needs immediately:

- **The design is the default — nothing conditional to know about.** `data/themes/zara-study.css`
  (the former reversible prototype theme) has been merged into `wwwroot/css/site.css` and deleted;
  `data/config/userConfig.json`'s `UserTheme` has been reset to `""`. The custom-theme-upload
  mechanism (`Controllers/ThemeController.cs`, `/css/theme.css`) still works exactly as it did
  originally in LubeLogger — it's just no longer where *this* design lives, and is available again
  for its original purpose (a genuinely separate, optional, user-uploaded alternate theme).
- All CSS for this initiative — tokens, palette, typography, component overrides, the Bootstrap
  component-local-variable fix (buttons/checkboxes/dropdowns), everything — is now in the single
  tracked `wwwroot/css/site.css` (2119 lines). No other file needs to be consulted to know what's
  currently active.
- Several Razor views and one C# controller/model have real, tracked, additive edits (documented per-
  increment in UI_TRANSITION.md) — unrelated to the theme-file promotion, already tracked in git
  as of the normal commit for this work.
- Self-hosted assets added under `wwwroot/lib/`: Fraunces + Work Sans (full 4-axis variable builds,
  re-sourced from the type foundry directly after discovering Google's default delivery strips SOFT/
  WONK axes), Flatpickr (replaced bootstrap-datepicker entirely), Phosphor Icons (replaced Bootstrap
  Icons via a CSS-only override layer, ~105 hand-mapped glyphs).
- User provided the actual print PDF (`C:\Users\muham\Downloads\Magneto_Spring_2024.pdf`, 220 pages)
  as primary-source design reference after web-only research kept producing a reskin instead of a
  structural translation. All 220 pages were reviewed (contact sheets + full-res deep-dives on the
  richest examples - see UI_TRANSITION.md for the concrete, directly-observed findings and exactly
  which patterns have and haven't been applied yet).
- Dev workflow used throughout this initiative: `dotnet build` -> `dotnet test Tests/
  CarCareTracker.Tests.csproj` -> kill any running `dotnet run` process -> restart with `dotnet run
  --urls http://localhost:5299 --no-build` in the background -> curl-verify against real vehicle data
  (vehicleId=1 in this dev environment) wherever possible. CSS-only changes need no rebuild (served
  as static files); any `.cshtml`/`.cs` change needs the full cycle. No browser tool exists in this
  environment - curl/build/test is the verification ceiling; anything visual/interactive is flagged
  as unverified rather than assumed correct, and the user has been doing live browser review between
  batches of work.
- Do not assume a "reskin" (new colours/fonts applied to unchanged Bootstrap structure) satisfies the
  brief - the user explicitly rejected that twice. The standing goal is structural/layout translation
  grounded in specifically-cited pages/patterns from the research, not a generic "editorial" vibe.

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
- Automated tests: `dotnet test Tests/CarCareTracker.Tests.csproj` from the repo root (or anywhere -
  content root is found by walking up for CarCareTracker.csproj, not by invocation directory). Fully
  isolated from real data; safe to run anytime.
