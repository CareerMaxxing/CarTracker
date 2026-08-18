# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 15 — Remote Access & Persistent Hosting (Increment 2 complete, Increment 3
                     next)
Current task:       PHASE-15-02 (see docs/execution/PHASE_15.md) — complete: publish + carry over
                     real data + register as a Windows Service bound to 127.0.0.1:5299
Status:             Increments 1-2 complete. User asked to plan phone access with live sync to the PC
                     app; since this is a single-server architecture with SignalR already
                     broadcasting live updates to every connected client, "sync" needs no new
                     data-layer work - only reachability. User explicitly authorized revisiting
                     CLAUDE.md's locked "localhost only/no cloud deployment" and "no cloud sync"
                     decisions, and chose Tailscale (private WireGuard network) over LAN-only access
                     or public-internet hosting via AskUserQuestion. Plan (4 increments: Windows
                     Service hosting, Tailscale + Kestrel loopback binding, auth enablement, PWA
                     install verification) was written via EnterPlanMode/ExitPlanMode and approved.
                     Increment 1: added Microsoft.Extensions.Hosting.WindowsServices +
                     UseWindowsService(); found and fixed a real gap not in the original plan - every
                     data path in this app (StaticHelper.DbName/UserConfigPath/ServerConfigPath) is
                     CWD-relative, and the Windows Service Control Manager defaults to a System32
                     working directory, so a service deployment would have silently created data/
                     under System32 without a WindowsServiceHelpers.IsWindowsService()-gated
                     Directory.SetCurrentDirectory(AppContext.BaseDirectory) fix; also added
                     DataProtection .PersistKeysToFileSystem("data/keys"). Committed as ab97b66/
                     7f99541, pushed. Increment 2: published to C:\Services\CarTracker (user's choice
                     of two offered install-path options), confirmed the dev repo's data/ contains
                     real vehicle data (224KB db, not test fixtures - user confirmed) and copied it
                     over once (user explicitly confirmed carrying it over as the live copy, dev
                     repo's original left untouched as fallback), pre-wrote
                     data/config/serverConfig.json's Kestrel section to bind 127.0.0.1:5299 only
                     (verified via netstat before involving the user), then handed the user exact
                     sc.exe commands to run in an elevated PowerShell (agent's shell confirmed
                     non-admin via WindowsPrincipal check first, didn't attempt and fail). User ran
                     them (screenshot showed CreateService SUCCESS + START_PENDING); agent
                     independently re-verified (not just trusted the screenshot) via sc.exe query
                     (STATE: 4 RUNNING), curl /health, and curl /api/vehicles (real BMW Z4 record
                     confirmed, not an empty db). Not yet committed to git (no code changes in this
                     increment, only external install/service-registration + docs updates pending).
Last completed:      Phase 14 Increment 3 (accessibility - modal aria-labelledby), see prior entries
                     in docs/execution/PHASE_14.md. Phase 14's remaining areas (icon-button labels,
                     keyboard nav, form labels, alt text, mobile/responsive validation, performance)
                     are still open, not abandoned - Phase 15 was started because the user raised a
                     new, higher-priority request (phone access), not because Phase 14 finished.
Next task:           Phase 15 Increment 3: install Tailscale on the PC and the user's phone (same
                     tailnet), run `tailscale serve https / http://127.0.0.1:5299` on the PC, and
                     verify reachability from the phone with home wifi OFF (mobile data only, to
                     prove it's actually tailnet reachability, not accidental LAN access). Needs the
                     user physically present with their phone - NOT something to attempt unattended.
                     After that: Increment 4 (turn on auth via Settings UI) and Increment 5 (confirm
                     PWA "Add to Home Screen" install from the tailnet URL).
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
                     process/service, hand-edit EnableAuth:false back, restart.
Last validation:     dotnet build (0 errors, 0 new warnings - 224 pre-existing nullable warnings
                     unchanged); dotnet test Tests/CarCareTracker.Tests.csproj (10/10 passing, no
                     regression); dotnet run --urls http://localhost:5299 --no-build (background) +
                     curl http://localhost:5299/health returned {"status":"pass",...}, and
                     data/keys/key-<guid>.xml was created, confirming the DataProtection change works
                     and the interactive dev workflow is unaffected — 2026-08-18.
Last validation (Increment 2): sc.exe query CarTracker → STATE: 4 RUNNING; curl
                     http://127.0.0.1:5299/health → {"status":"pass",...}; curl
                     http://127.0.0.1:5299/api/vehicles → real BMW Z4 record (id 1), confirming the
                     service is serving the carried-over real data, not a fresh database; netstat
                     confirmed binding to 127.0.0.1:5299 only, no wildcard/non-loopback address —
                     2026-08-18.
Last commit:         ab97b66/7f99541 — "Phase 15 Increment 1" (code) — 2026-08-18. Increment 2 (this
                     entry) has no code changes, only docs - to be committed alongside this STATE.md
                     update.
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
