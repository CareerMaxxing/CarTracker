# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 14 — V1 Hardening (Increment 3: Accessibility - modal aria-labelledby)
Current task:       PHASE-14-03 (see docs/execution/PHASE_14.md) — wire aria-labelledby on every
                     Bootstrap modal
Status:             Complete. A background code-level accessibility audit (no browser available)
                     found 4 issue categories across the Views tree; user chose the narrowest scope
                     (modal aria-labelledby only) of the options offered. All 41 modal shells traced
                     to their real content-providing partial via the actual AJAX call chain (not
                     guessed from naming), 39 wired with correct aria-labelledby/aria-label, 2 left
                     unresolved (documented, not guessed at). The batch-fix process itself surfaced
                     two real pre-existing bugs (duplicate ids that could collide on the same page)
                     and fixed both. Verified: dotnet build (0 errors), full test suite (10/10
                     passing - confirms this UI-only change didn't regress app logic), and rendered
                     HTML spot-checked across Vehicle/Home/Admin/Settings pages confirming every
                     aria-labelledby resolves to a real id in the actually-rendered content.
Last completed:      Phase 14 Increment 2 (automated tests) finished and pushed. User said "start
                     phase 14" (ambiguous re: which increment - interpreted as "continue," picked
                     accessibility as the next self-sufficient increment given no browser tool
                     exists here, matching the established pattern of making a reasonable call
                     rather than re-asking when the user's intent to continue was already clear).
                     Ran a background Explore-agent code audit first (read-only, no changes) to
                     scope the work before touching anything, then presented findings and let the
                     user pick the fix scope rather than assuming "audit found it, so fix it all."
Next task:           Phase 14 takes increments - icon-only button labels, keyboard-nav on primary
                     click surfaces, form input labels, image alt text, mobile/responsive validation,
                     and performance review all remain open (see DEFERRED.md's new Accessibility
                     section for the un-fixed findings with file pointers). Ask the user for the next
                     priority, or whether to pause here.
Known blockers:      1. No browser/screenshot tool in this environment - accessibility work here was
                        done via static code audit + curl-verified rendered HTML, not live
                        screen-reader/keyboard testing. Real assistive-tech verification would need
                        the user's help if that level of confidence is ever wanted.
                     2. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items, including this increment's own un-fixed
                        accessibility findings (icon buttons, keyboard nav, form labels, alt text,
                        2 unresolved modals) with concrete file pointers so a future increment can
                        start without re-auditing.
Open decisions:      What to prioritize next within Phase 14 (or elsewhere) - ask the user rather
                     than assume. Standing instruction: verify and approve each increment/phase
                     before the next one starts.
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
                     anytime.
Last validation:     dotnet build (0 errors); dotnet test (10/10 passing); rendered HTML spot-checked
                     across Vehicle/Index tab partials (service, odometer), Home/Index, Admin/Index,
                     Home Settings - every aria-labelledby confirmed resolving to a real id in the
                     actually-rendered content; confirmed no remaining problematic duplicate
                     modal-title ids anywhere in the Views tree (the 2 apparent duplicates that remain
                     are both benign - different pages that never coexist, or mutually-exclusive
                     @if/else branches within one partial) — 2026-08-17.
Last commit:         fc8d7f4 — "Phase 14 (accessibility): wire aria-labelledby on all Bootstrap
                     modals" — pushed 2026-08-17, awaiting user confirmation before the next
                     increment.
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
