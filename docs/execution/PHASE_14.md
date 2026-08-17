# PHASE_14 — V1 Hardening

Explicitly open-ended per the roadmap (tests, accessibility, responsive/mobile validation,
performance, security review, error handling). Rather than attempt all of it at once, the user was
asked to prioritize each time; they chose **security review** first, then **automated tests**. This
document covers both increments so far. Other V1 Hardening areas (accessibility, mobile/responsive
validation, performance) remain open - not deferred/declined, just not yet started - and should get
their own increments here under this same PHASE_14.md rather than a new phase number.

## Increment 1: Security review

### Task packet

```
TASK ID: PHASE-14-01
TITLE: Security review of Phases 1-12's changes
OBJECTIVE: Find and, where safe to do so without a larger architectural fork, fix real
  vulnerabilities in the code introduced or extended during this project - prioritizing the areas
  most changed (file/document handling, cross-vehicle search, government data, cleanup/backup).
INPUTS: Program.cs (static file serving, auth pipeline), Controllers/FilesController.cs (upload
  handling), all Phase 8-12 additions (government data, odometer, documents, search, reliability).
ALLOWED SCOPE: Read-only auditing across the whole delta from upstream LubeLogger; empirical
  verification of suspected issues (not just static reading) using throwaway/reversible test data;
  fixes for confirmed, concretely-exploitable findings, scoped to the minimum change that closes
  each one - explicitly flagged to the user before implementing, since both confirmed findings were
  architectural/security-sensitive decisions per CLAUDE.md's mandatory stop conditions.
NON-SCOPE: A full penetration test or automated security scanner run; CSRF token infrastructure
  (noted as a lower-confidence, not-empirically-confirmed observation, not investigated deeply -
  this app's REST-API-first design already assumes a different threat model for state-changing
  calls); rewriting the authentication scheme itself; Postgres-specific query review beyond a
  spot-check (parameterization confirmed correct, no evidence of a different pattern elsewhere).
IMPLEMENTATION REQUIREMENTS (for confirmed findings only, after user sign-off):
  - Auth bypass: gate /documents, /images, /temp with real middleware that short-circuits (returns
    without calling next()) before UseStaticFiles ever runs, rather than relying on
    StaticFileOptions.OnPrepareResponse (which fires after the middleware has already committed to
    serving 200 + the file body, so Response.Redirect() there cannot stop the bytes from being
    sent). Add the missing app.UseAuthentication() call so HttpContext.User is actually populated
    before this gate (and before UseAuthorization() much later) - previously nothing populated it
    for the static-file pipeline at all, so the existing IsAuthenticated check inside
    OnPrepareResponse was evaluating against an always-default identity for every request,
    authenticated or not.
  - Upload validation: block a fixed extension list (script/executable/active-content types -
    .html/.htm/.xhtml/.svg/.js/.mjs/.exe/.dll/.bat/.cmd/.com/.msi/.ps1/.psm1/.sh/.jar/.php variants/
    .asp/.aspx/.jsp/.hta/.vbs/.wsf/.scr/.cpl/.jse/.vbe) at upload time, in the single shared
    UploadFile method both HandleFileUpload and HandleMultipleFileUpload already funnel through -
    reject silently (empty response, consistent with this codebase's existing "empty response =
    no-op" convention already relied on by uploadFileAsync's caller in shared.js), not with an error
    that would require a wider response-contract change.
DELIVERABLES: A working, verified fix for both confirmed findings; a written report of everything
  found (including lower-severity/not-acted-on observations) for the user's record.
ACCEPTANCE CRITERIA:
  - An unauthenticated request to a real document/image/temp file, with EnableAuth on, returns no
    file content (previously it did, despite a 302 status).
  - The same request in the default EnableAuth=off (local dev) mode is unaffected - no regression.
  - A public wwwroot asset (e.g. /css/site.css) remains reachable anonymously in both modes.
  - Uploading a .html/.svg file is rejected (no file written, empty response); uploading a normal
    file (.txt/.pdf/etc.) still succeeds exactly as before.
  - A mixed multi-file upload (one blocked type, one normal type) keeps only the normal one.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl: reproduced the auth-bypass bug empirically first (a real PDF attachment
  was retrievable in full despite a 302 redirect response, with EnableAuth on) before fixing it, to
  avoid fixing a hypothetical rather than a demonstrated issue; re-ran the same request after the fix
  and confirmed Content-Length: 0; confirmed no regression with EnableAuth off and for public static
  assets; reproduced the unrestricted-upload gap (uploaded a .html file with an alert() payload,
  confirmed it was accepted and would have been served same-origin) before fixing it; re-tested after
  the fix with malicious and legitimate files, including a mixed multi-file batch.
STOP CONDITION: Both fixes verified via curl before considering this increment done; real vehicle
  data read-only throughout (the one genuine document tested against was never modified or deleted).
```

### What was done

1. Started from the areas of highest concrete risk given what Phases 1-12 actually built: file/
   document handling (heavily extended in Phase 10), the new cross-vehicle search surface
   (Phase 11), and the new maintenance/cleanup endpoints (Phase 12) - rather than a generic sweep,
   since a security review is most valuable when grounded in what changed.
2. Verified `Logic/UserLogic.cs`'s `FilterUserVehicles` (reused as-is by Phase 11's
   `SearchRecordsAcrossVehicles`) correctly scopes results to vehicles the caller actually has
   access to, including the household-parent-user case - no IDOR introduced by the new cross-vehicle
   search surface.
3. Confirmed `SearchResult.RecordType` (changed from `ImportMode` to `string` in Phase 11) is never
   assigned from user-controlled input anywhere - always a fixed literal from a small closed set -
   so rendering it inside a single-quoted JS string in `_GlobalSearchResult.cshtml`'s `onclick`
   attribute isn't exploitable today, though it's worth noting for future maintainers that this
   field no longer has compiler-enforced protection against that class of mistake.
4. Spot-checked Postgres query construction (`PGPartPurchaseDataAccess` and, by the same pattern,
   the rest of `External/Implementations/Postgres/`) - confirmed parameterized queries
   (`NpgsqlParameter`/`AddWithValue`) throughout, no string-concatenated user input into SQL.
5. Found, and empirically confirmed (not just read and assumed) two real, pre-existing issues in the
   static-file/upload subsystem that Phase 10 directly extended:
   - **Auth bypass on `/documents`, `/images`, `/temp`**: the existing
     `if (!userIsAuthenticated) Response.Redirect("/Login")` inside each route's
     `StaticFileOptions.OnPrepareResponse` only appends response headers - it cannot stop
     `StaticFileMiddleware` from writing the file body, because by the time `OnPrepareResponse`
     runs, the middleware has already committed to serving 200 + the file content. Verified live:
     with `EnableAuth` on, an unauthenticated request for a real document on the user's actual
     vehicle came back `302 Found` with the complete PDF in the response body. Same pattern affects
     `/temp`, which is worse in one respect - full database backup ZIPs live there transiently.
     Root cause compounded by a second, related gap: `app.UseAuthentication()` was never called
     anywhere in the pipeline, so `HttpContext.User` was never actually populated before this check
     ran for ANY request (the check's `IsAuthenticated` was always false, for authenticated and
     anonymous callers alike - it just didn't matter before because the redirect never blocked
     anything anyway).
   - **No file-type validation on upload**: `FilesController.UploadFile` (the method both
     `HandleFileUpload` and `HandleMultipleFileUpload` funnel through) accepted any file type. Since
     `/documents`/`/images` serve files back same-origin with a content-type inferred from
     extension, an uploaded `.html` or `.svg` file with embedded script becomes stored XSS reachable
     by anyone who opens that "attachment" - a real risk given this app's collaborator/household
     model (one user's malicious upload, another user's session). Verified live: uploaded an `.html`
     file containing an `alert()` payload and confirmed it was accepted and would have been served
     as-is.
6. Presented both findings to the user before fixing - per `CLAUDE.md`'s mandatory stop condition
   for security-sensitive architectural decisions - with a recommended fix for each. User approved
   both.
7. Fixed the auth bypass: added `app.UseAuthentication()` early in the pipeline, and a new inline
   middleware immediately after it that checks `/documents`/`/images`/`/temp` path prefixes and
   short-circuits (`return` without calling `next()`) on an unauthenticated request, placed before
   any `UseStaticFiles` call. Removed the now-dead (never reachable, since the new gate already
   redirected) `IsAuthenticated` checks from each route's `OnPrepareResponse`, keeping only the
   `Cache-Control: no-store` header logic they also set.
8. Fixed the upload gap: added a fixed blocklist of script/executable/active-content extensions,
   checked in `UploadFile` before anything is written to disk. Rejected files return an empty
   string/get filtered out of the multi-file response array - matching this codebase's existing
   convention (already relied on by `uploadFileAsync`'s caller in `shared.js`, which already checks
   `response.trim() != ''` before using a result) rather than introducing a new error-response shape.
9. Verified both fixes via curl:
   - `EnableAuth` on, unauthenticated request to the same real document: `302 Found`,
     `Content-Length: 0` (previously the full file).
   - `EnableAuth` off (default/local dev mode): the same document still loads normally (no
     regression) - confirms the fake-admin dev identity still authenticates correctly through the
     newly-added `UseAuthentication()` call.
   - A public `wwwroot` asset (`/css/site.css`) remains reachable anonymously in both modes -
     confirms the new gate is correctly scoped to only the three protected roots.
   - Uploading `.html`/`.svg` payloads: rejected (empty response), no file written to disk.
   - Uploading a normal `.txt` file: unaffected, succeeds exactly as before.
   - A mixed multi-file upload (one blocked, one normal): only the normal file appears in the
     response.
   - `dotnet build`: 0 errors throughout.
10. Lower-confidence/not-acted-on observations, recorded for completeness rather than investigated
    further this round: no CSRF token infrastructure was found on the MVC cookie-session endpoints
    (plausible by design, since this app is built API-first with Basic-Auth/API-key support baked
    into nearly every write action - a different threat model than a typical browser-only app - but
    not deeply verified either way); no Content-Security-Policy header anywhere, which would have
    been useful defense-in-depth against the (now-closed) stored-XSS angle above.

### Result

Both confirmed findings fixed and verified. Real vehicle data was read-only throughout (the one
genuine document used for testing was never modified or deleted); all other verification used
throwaway uploads cleaned up afterward. Remaining Phase 14 areas (tests, accessibility, mobile/
responsive, performance) are open for a future increment.

## Increment 2: Automated test project

### Task packet

```
TASK ID: PHASE-14-02
TITLE: Stand up the automated test project deferred since Phase 7
OBJECTIVE: Turn the concrete, already-documented Phase 7 findings (DEFERRED.md's "Test
  infrastructure" section) into a real, working xUnit + WebApplicationFactory integration test
  project, isolated from the developer's real data/ directory, with an initial suite covering the
  highest-risk flows already identified across this project's phases.
INPUTS: docs/execution/DEFERRED.md's Test infrastructure section (the concrete findings from Phase
  7's investigation - DbName's CWD-relative resolution, the need for `public partial class Program`,
  parallel-test-safety concerns), Program.cs, CarCareTracker.csproj.
ALLOWED SCOPE: `public partial class Program { }` in Program.cs; a new CarCareTracker.Tests project
  (xUnit + Microsoft.AspNetCore.Mvc.Testing) added to the solution; a shared WebApplicationFactory-
  based test fixture with correct data isolation; an initial test suite covering flows already
  identified as high-risk in prior phase docs (Phase 7's idempotency fix, Phase 12's two reliability
  bugs, Phase 9's regression warning, Phase 14 Increment 1's upload validation fix); excluding the
  new Tests/ directory from the main project's compilation (a build-glob conflict discovered while
  implementing, not anticipated in the original DEFERRED.md notes).
NON-SCOPE: Exhaustive coverage of every endpoint/flow in the app (a full test-writing pass across 14
  phases of work is its own multi-session effort, not a single increment); CI/CD pipeline wiring (no
  CI system is configured for this repo yet); performance/load testing (a different Phase 14 area).
IMPLEMENTATION REQUIREMENTS:
  - Program.cs: append `public partial class Program { }` after `app.Run()` (top-level statement
    programs generate this class `internal` by default; a separate test assembly needs it public to
    reference as `WebApplicationFactory<Program>`'s type parameter).
  - CarCareTracker.csproj: exclude `Tests/**/*.cs` (and Content/None) from the main project's
    default recursive globs, since SDK-style projects otherwise compile every .cs file under the
    project root including a nested test project's xUnit-only sources - not something DEFERRED.md's
    original Phase 7 notes anticipated, discovered empirically while wiring the new project in.
  - CarTrackerWebApplicationFactory: switches the process's current directory to a fresh temp folder
    per factory instance before the host builds (StaticHelper.DbName/UserConfigPath/etc. are CWD-
    relative, not ContentRootPath-relative, per Phase 7's finding) so tests never touch the real
    developer's data/ directory; explicitly resolves the app's actual content root (wwwroot/Views) by
    walking up from AppContext.BaseDirectory looking for CarCareTracker.csproj by name, rather than
    trusting WebApplicationFactory's own auto-detection (which assumes a "solution/ProjectName/
    ProjectName.csproj" layout this repo's flat structure doesn't match) or Directory.
    GetCurrentDirectory() (which `dotnet test`'s VSTest host sets to the test assembly's own bin
    output folder before any test code runs, not wherever `dotnet test` was invoked from - neither
    assumption anticipated in the original notes either).
  - One shared CarTrackerWebApplicationFactory instance via ICollectionFixture, with every test class
    tagged [Collection("CarTracker")] to force full serialization - Directory.SetCurrentDirectory is
    process-wide, so parallel instances would race (matches DEFERRED.md's flagged parallel-safety
    concern; serialization was chosen as the simpler of its two suggested strategies over per-test
    temp directories, appropriate for this project's current test volume).
DELIVERABLES: A working test project, 4 test classes covering idempotency/reliability/security/
  regression-warning flows, all passing against the real application code (not a stub).
ACCEPTANCE CRITERIA:
  - `dotnet test` runs the full suite against real HTTP endpoints and a real (isolated) LiteDB
    instance, with 0 failures.
  - No test run touches the developer's real data/ directory or real vehicle - verified directly,
    not just inferred from the isolation design.
  - The main app's normal `dotnet build`/`dotnet run` are unaffected by the new project's presence.
  - Each test covers a real, previously-identified risk (not an arbitrary smoke test): Phase 7's
    idempotent plan completion, both Phase 12 Part/PartPurchase reliability bugs, Phase 14
    Increment 1's upload-extension blocklist, and Phase 9's odometer regression warning (including
    the HasOdometerAdjustment exemption).
VALIDATION COMMANDS:
  dotnet build (main solution, confirms no regression from the new project's presence)
  dotnet test Tests/CarCareTracker.Tests.csproj (the new suite itself)
  dotnet run (confirms the real app still starts and serves the real vehicle normally after
  Program.cs's and CarCareTracker.csproj's changes)
STOP CONDITION: All 10 tests passing, confirmed isolated from real data, real app confirmed
  unaffected, changes committed.
```

### What was done

1. Re-read `DEFERRED.md`'s existing Phase 7 test-infrastructure findings rather than re-deriving them
   from scratch - they already correctly identified the CWD-relative `DbName` path, the need for a
   public partial `Program` class, and the parallel-test-safety hazard.
2. Scaffolded `Tests/CarCareTracker.Tests.csproj` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`,
   referencing the main project), added it to `CarCareTracker.sln`.
3. Hit a build error immediately: the main `CarCareTracker.csproj`'s default SDK-style recursive glob
   was also compiling every `.cs` file under the newly-created `Tests/` subdirectory into the *main*
   app (which doesn't reference xUnit), since `Tests/` is nested under the main project's own root.
   Fixed by explicitly excluding `Tests/**/*.cs` (and `Content`/`None`) from the main project's globs
   - a real gap in the original Phase 7 notes, only discoverable by actually wiring the project in.
4. Built `CarTrackerWebApplicationFactory`, implementing DEFERRED.md's CWD-isolation finding (switch
   the process's current directory to a fresh temp folder before the host builds, so `data/
   cartracker.db` and friends resolve there instead of the real project's `data/`).
5. Hit two further, unanticipated problems getting the host to actually start under test, both
   diagnosed empirically (adding temporary diagnostic output and re-running, not guessed at):
   - `WebApplicationFactory`'s built-in content-root auto-detection guessed
     `<repo>\CarCareTracker\` (assuming a "solution/ProjectName/ProjectName.csproj" folder layout)
     instead of `<repo>\` itself, since this repo's actual layout is flat
     (`CarCareTracker.csproj` lives directly in the repo root). This produced a
     `DirectoryNotFoundException` before any of my own code ran.
   - Switching to an explicit `ConfigureWebHost` + `UseContentRoot(Directory.GetCurrentDirectory())`
     call produced a *different* failure (`WebRootPath` came back null, crashing
     `StaticHelper.CheckMigration`) - diagnostic output revealed `Directory.GetCurrentDirectory()`
     at that point was already `Tests\bin\Debug\net10.0` (the test assembly's own build output
     folder), because `dotnet test`'s VSTest host sets the process's working directory there before
     any test code executes, not wherever `dotnet test` was actually invoked from.
   - Fixed by walking up from `AppContext.BaseDirectory` looking for `CarCareTracker.csproj` by name
     - the one content-root-finding approach that depends on neither WebApplicationFactory's
     layout assumption nor the process's current-directory state at any point in time.
6. Wrote 4 test classes (10 tests total), each covering a specific, previously-identified risk rather
   than a generic smoke test:
   - `PlanCompletionIdempotencyTests` (Phase 7): completes the same plan record 3 times, asserts
     exactly 1 resulting `ServiceRecord`.
   - `PartPurchaseReliabilityTests` (Phase 12): asserts deleting a vehicle also deletes its
     `PartPurchase` records; asserts a `PartPurchase`'s attachment survives a deep-clean sweep.
   - `FileUploadSecurityTests` (Phase 14 Increment 1): asserts `.html`/`.svg`/`.js` uploads are
     rejected, a normal `.txt` upload succeeds, and a mixed multi-file batch keeps only the
     legitimate file.
   - `OdometerRegressionTests` (Phase 9): asserts a lower-mileage entry is flagged but still saved,
     and that `HasOdometerAdjustment` suppresses the flag.
7. Debugged one test failure that turned out to be a test-authoring mistake, not an app bug: the
   idempotency test's plan-add payload used `priority: "Medium"`, which isn't a valid `PlanPriority`
   value (`Critical`/`Normal`/`Low` are) - the API correctly rejected it with 400; fixed the test.
8. Verified the full suite passes twice in a row (stability, not a one-off pass), confirmed the real
   app's `dotnet build`/`dotnet run` are unaffected by the new project's presence, and confirmed
   directly (not just by isolation design) that the real vehicle and `data/` directory were untouched
   by any test run.
9. Noted one minor, accepted limitation: `LiteDBHelper` doesn't implement `IDisposable`, so its
   underlying `LiteDatabase` file handle isn't guaranteed released by the time the test host disposes
   - the fixture's temp-directory cleanup is therefore best-effort and can leave an empty-ish folder
   in the OS temp directory after a run. Never contains real data (only throwaway test-run state),
   and not worth changing the app's production DI lifecycle just for test cleanup - left as a known,
   low-cost trade-off rather than "fixed."

### Result

Complete. A real, working, isolated integration test project now exists, covering four previously-
identified high-risk flows with 10 passing tests. Not exhaustive coverage of the whole app - a
starting point per DEFERRED.md's own framing, ready for more tests to be added incrementally as
future work touches other areas.

## Increment 3: Accessibility — modal `aria-labelledby` wiring

A background code-level audit (no browser available in this environment) surveyed the Views tree
for accessibility issues, since a code audit is the one accessibility technique that doesn't require
live rendering to verify. It found four issue categories: icon-only buttons/links with no accessible
name (40+ files), custom `onclick` divs/spans/table-rows with no keyboard support (~29 files), form
inputs without labels (localized), and Bootstrap modals missing `aria-labelledby` (all ~41 of them).
Given the scale, the user was asked how much to fix now and chose the narrowest, highest-confidence
option: modal `aria-labelledby` wiring only. The other three categories are recorded in
`DEFERRED.md` for a future increment.

### Task packet

```
TASK ID: PHASE-14-03
TITLE: Wire aria-labelledby on every Bootstrap modal
OBJECTIVE: Give every modal dialog in the app a screen-reader-announced purpose on open, without
  changing any visible UI.
INPUTS: The accessibility audit's modal findings; every Views/**/*.cshtml file containing
  `class="modal fade"` or `class="modal-title"`.
ALLOWED SCOPE: Adding `id` to each modal's title element (or reusing one if it already had one -
  several already did) and `aria-labelledby` (or `aria-label` for the few modals with no title text
  at all) to the corresponding outer `.modal.fade` shell. Fixing any id collision surfaced along the
  way (a title `id` reused by two different modals is worse than no `id` - aria-labelledby would
  resolve to whichever element happens to come first in the DOM).
NON-SCOPE: Icon-only buttons, keyboard-operability of custom click handlers, unlabeled form inputs,
  image alt text - all real findings from the same audit, deferred to a future increment by explicit
  user choice, not overlooked. A generic Parts-catalog-style `vehicleDataTableModal` (reused for
  several different report views with no single fixed title) and `inputSuppliesModal` (JS-templated
  content, no clean static title to hook) were left unresolved rather than force a bad pairing -
  noted in DEFERRED.md instead of guessed at.
IMPLEMENTATION REQUIREMENTS:
  - This app's modals are almost universally AJAX-loaded: a persistent outer shell
    (`<div class="modal fade" id="XModal">` with an empty `<div class="modal-content"
    id="XModalContent">`) lives in one file, populated at runtime from a separate partial view (e.g.
    `Service/_ServiceRecordModal.cshtml`) that contains the actual `.modal-title`. Both the shell's
    `id` and the partial's title `id` are static strings baked into the Razor source, so
    `aria-labelledby` on the shell can safely reference an `id` that doesn't exist in the DOM until
    that content loads - by the time a modal is actually opened, the referenced content is already
    there (every `showXModal()` JS function loads content before calling `.modal('show')`).
  - Correctly pair each of the 41 modal shells with its actual content-providing partial by tracing
    the real AJAX call chain (JS `$.get(url).html(data)` → matching controller action → its
    `PartialView("...")` call), not by guessing from naming convention alone - several pairings
    weren't obvious from names (e.g. `inspectionRecordTemplateModal`'s content partial is named
    `_InspectionRecordTemplateSelector`, not anything containing "Modal").
  - Prefer an already-existing `id` on a modal's title over inventing a new one, to avoid touching
    more than necessary - several modals already had one (apparently for other purposes) that just
    needed to be wired up on the outer shell.
DELIVERABLES: aria-labelledby (or aria-label) added to every modal shell that could be confidently
  paired with real content; two real id-collision bugs found and fixed along the way.
ACCEPTANCE CRITERIA:
  - Every modal's outer shell has aria-labelledby pointing at an id that actually exists in its
    paired content partial (or aria-label, for the handful with no title text at all).
  - No two modal-title elements that can appear on the same page share the same id.
  - The main app's build and the full automated test suite are both unaffected.
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run, then curl the rendered HTML for a representative sample across different pages
  (Vehicle/Index tab partials, Home/Index, Admin/Index, Home Settings) confirming each
  aria-labelledby value matches a real id in the corresponding rendered content.
STOP CONDITION: Build and test suite green, spot-checked rendering confirms correct pairing across a
  representative sample, changes committed.
```

### What was done

1. Enumerated all 41 distinct modal shell `id`s across the Views tree (`grep` for
   `class="modal fade"`), then wrote a small Python analysis pass to classify each as
   AJAX-content-loaded (nearly all of them) versus having an inline title already.
2. Traced the real content-providing partial for each shell by following the actual runtime chain -
   JS function → AJAX URL → controller action → its `PartialView(...)` call - rather than guessing
   from filename similarity, since several pairings weren't obvious from naming alone.
3. Wrote a batch Python script (not 40+ manual edits) that, per modal: added an `id` to the content
   partial's `.modal-title` (or detected and reused an existing one), then added the matching
   `aria-labelledby` to every outer shell referencing that content.
4. The script's own output surfaced two real, pre-existing bugs, both fixed:
   - `_AccountModal.cshtml` and `_AttachmentPreview.cshtml` used the exact same `id`
     (`updateAccountModalLabel`) on their title elements - clearly `_AttachmentPreview.cshtml`'s was
     copy-pasted from the account modal and never renamed. Since both can appear on the same page
     (`Home/Index.cshtml` has both), this was a real duplicate-id bug, not just an accessibility gap
     - fixed by giving `_AttachmentPreview.cshtml` its own unique id.
   - `Home/_Settings.cshtml`'s `tabReorderModal` used `id="translationEditorModalLabel"` on its own
     title - copy-pasted from the actual translation editor modal in the same file. Fixed the same
     way.
5. Handled three modals with no clean AJAX-loaded title to pair against individually rather than
   force a fit: `globalSearchModal` and `vehicleCustomWidgetsModal` have no title text at all (used
   `aria-label` matching their trigger button's own text instead); `tokenModal` is fully
   self-contained in one file (added both `id` and `aria-labelledby` directly there).
6. Left two modals unresolved, documented in `DEFERRED.md` rather than guessed at:
   `vehicleDataTableModal` (reused for several different report drill-down views with no single
   fixed title) and `inputSuppliesModal` (content built from JS-templated HTML strings, no clean
   static title to hook an id onto).
7. Verified: full `dotnet build` (0 errors) and the automated test suite (10/10 passing, confirming
   this UI-only change didn't regress any application logic); confirmed no remaining id collisions
   across the whole Views tree by grepping for duplicate `.modal-title` ids (the two benign
   "duplicates" that remain - `householdModalLabel` used by two admin-only vs. user-only modals that
   never appear on the same page, and `attachmentPreviewModalLabel` used by two mutually-exclusive
   `@if/else` branches within the same partial - are both non-issues); spot-checked rendered HTML
   across Vehicle tab partials, Home, Admin, and Settings pages, confirming every `aria-labelledby`
   resolves to a real id in the actually-rendered content.

### Result

Complete for the scope chosen. All confidently-pairable modals (39 of 41) now correctly announce
their purpose to screen readers on open, plus two real duplicate-id bugs fixed as a side effect of
the same pass. Icon-only buttons, keyboard-operability, and form-label gaps remain for a future
increment (see `DEFERRED.md`).
