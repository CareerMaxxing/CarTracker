# PHASE_14 — V1 Hardening

Explicitly open-ended per the roadmap (tests, accessibility, responsive/mobile validation,
performance, security review, error handling). Rather than attempt all of it at once, the user was
asked to prioritize; they chose **security review** first. This document covers that increment.
Other V1 Hardening areas (automated tests, accessibility, mobile/responsive validation, performance)
remain open - not deferred/declined, just not yet started - and should get their own increments here
under this same PHASE_14.md rather than a new phase number.

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
