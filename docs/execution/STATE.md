# STATE.md

Persistent execution state. Read this before starting any work — do not assume state from
conversation history.

```
PROJECT STATUS
Current phase:      Phase 14 — V1 Hardening (Increment 1: Security Review)
Current task:       PHASE-14-01 (see docs/execution/PHASE_14.md) — security review of Phases 1-12
Status:             Complete. Found and fixed two real, empirically-confirmed vulnerabilities (not
                     just theoretical): a static-file auth bypass on /documents,/images,/temp, and
                     unrestricted file-type upload enabling stored XSS. Both explicitly approved by
                     the user before implementing (security-sensitive architectural decisions per
                     CLAUDE.md's mandatory stop conditions), both curl-verified fixed with no
                     regression to the default EnableAuth=off mode or to public static assets. Real
                     vehicle data was read-only throughout testing.
Last completed:      Phase 13 confirmed deferred (AI/OCR, no implementation, per CLAUDE.md's locked
                     decision). User then chose "security review" as Phase 14's first priority (of
                     four offered: security/tests/error-handling/accessibility). Findings:
                     (1) StaticFileOptions.OnPrepareResponse's Response.Redirect() cannot stop
                     StaticFileMiddleware from writing a file's body - it fires after the middleware
                     has already committed to serving 200 + content. Verified live: an unauthenticated
                     request for a real document, with EnableAuth on, returned the full PDF despite a
                     302 status. Compounded by app.UseAuthentication() never being called anywhere,
                     so HttpContext.User was never populated for the static-file pipeline at all -
                     the IsAuthenticated check was evaluating a permanently-default identity.
                     (2) FilesController.UploadFile accepted any file type; since /documents and
                     /images serve files back same-origin with content-type inferred from extension,
                     an uploaded .html/.svg with embedded script becomes stored XSS reachable by any
                     collaborator who opens it. Verified live: uploaded an alert()-payload .html file,
                     confirmed it was accepted. Fixed both after user sign-off: added
                     app.UseAuthentication() + a short-circuiting gate middleware before
                     UseStaticFiles for the three protected roots; added a fixed extension blocklist
                     in UploadFile (script/executable/active-content types), rejecting silently
                     (empty response) to match the existing convention already relied on by
                     uploadFileAsync's caller in shared.js.
Next task:           Phase 14 takes increments, not one pass - remaining areas (automated tests,
                     accessibility, mobile/responsive validation, performance) are open. Ask the user
                     which to prioritize next, or whether this is a good stopping point for now.
Known blockers:      1. No browser/screenshot tool in this environment - this increment was entirely
                        backend/pipeline-level and fully curl-verifiable; a live browser check isn't
                        especially valuable for it (no new UI surface).
                     2. No test project exists yet - investigated in Phase 7, concrete technical path
                        documented in DEFERRED.md, deferred again by explicit user choice at the time;
                        now also a candidate for a future Phase 14 increment if the user wants it.
                     3. See docs/execution/DEFERRED.md for the full consolidated list of
                        intentionally-punted items. This increment added: CSRF token infrastructure
                        (not deeply investigated, plausible-by-design given the API-first
                        Basic-Auth/API-key threat model, but unverified), Content-Security-Policy (no
                        header set anywhere, would need an inline-script/style audit first).
Open decisions:      What to prioritize next within Phase 14 (or elsewhere) - ask the user rather
                     than assume. Standing instruction: verify and approve each increment/phase
                     before the next one starts.
Do not:              Assume Phase 14 is "done" - it's an open-ended phase taken in increments; only
                     the security-review increment is complete. Do not implement CSRF tokens or a CSP
                     header without discussing first - both are real scope (the latter needs an
                     inline-script/style audit to avoid breaking the UI) and neither was requested
                     yet. Do not re-add the old OnPrepareResponse-based auth checks on the static
                     file routes - they're structurally incapable of blocking content (this was the
                     actual bug); any future change to those routes' auth must go through the gate
                     middleware in Program.cs, not StaticFileOptions callbacks. Do not assume
                     SQLite is available anywhere in this codebase. Do not assume a fresh vehicle/
                     user has any tabs visible beyond Dashboard - VisibleTabs defaults to
                     [Dashboard] only. Do not add a "MOT"/"Part"/etc. (any non-record-type) value to
                     the ImportMode enum - use a dedicated enum or a plain string instead. When any
                     controller does a "move files from temp"/reconstruct-UploadedFiles step, it MUST
                     explicitly copy every field it wants to keep. When adding a new entity type with
                     its own Files/attachments, wire it into GetVehicleDocuments/
                     DeleteVehicleRecords/ClearUnlinkedDocuments in Logic/VehicleLogic.cs (this
                     exact bug already happened once for real, for Part/PartPurchase - Phase 12).
                     Any enum embedded directly in a type used as a JSON request-body wire format
                     needs its own JsonStringEnumConverter. When calling record-add API endpoints for
                     testing, field names/casing are inconsistent across export models and dates must
                     match the server's locale (dd/mm/yyyy here). Note records require both
                     Description AND NoteText. Some fields (e.g. Vehicle.HasOdometerAdjustment) are
                     MVC-only, not exposed on the API's *ImportModel DTOs. Part is NOT vehicle-scoped
                     (global catalog) but PartPurchase IS (VehicleId, 0=shop-wide).
                     PartPurchase.QuantityRemaining must be set explicitly by the caller, never by
                     ToPartPurchase(). PlanRecord.ActualCost is preferred over Cost (estimate) by the
                     completion-conversion logic when non-zero. Government data is looked up by
                     Vehicle.LicensePlate, never VehicleIdentifier. OdometerRecord.Source must be
                     preserved (not reset to Manual) on manual edits of auto-inserted records. The
                     root/dev user's config (EnableAuth=false) reads directly from
                     data/config/userConfig.json but is cached in-memory for up to 1 hour - restart
                     the app after editing it. curl's `-d "key=/path/with/slashes"` can silently fail
                     to bind a form field in ways `--data-urlencode` or a query-string param won't.
Last validation:     dotnet build (0 errors); with EnableAuth temporarily enabled (userConfig.json
                     backed up first, restored byte-identical after, app restarted before and after
                     each toggle): unauthenticated request to a real document returned
                     Content-Length: 0 after the fix (previously the full file despite a 302);
                     EnableAuth=off mode confirmed unaffected (same document still loads normally);
                     /css/site.css confirmed still anonymous in both modes. Upload blocklist: .html
                     and .svg payloads with embedded script both rejected (empty response, no file
                     written); a normal .txt upload unaffected; a mixed multi-file batch kept only the
                     legitimate file. Real vehicle data read-only throughout (the one genuine document
                     used for the auth-bypass test was never modified or deleted) — 2026-08-17.
Last commit:         96d3bfa — "Phase 13: confirm AI/OCR deferred, no implementation" (this
                     increment's commit not yet made - pending user confirmation first).
```

## Environment notes for future sessions

- .NET 10 SDK (10.0.400) is installed via winget on this machine. If working from a fresh machine
  and `dotnet --list-sdks` is empty, install it first (`winget install Microsoft.DotNet.SDK.10`).
- Git remotes: `origin` = `https://github.com/CareerMaxxing/CarTracker.git` (push here),
  `upstream` = `https://github.com/hargata/lubelog.git` (fetch-only, for tracking upstream fixes).
- Local run: `dotnet run --urls http://localhost:5299` from the repo root; `data/` is auto-created
  and gitignored.
