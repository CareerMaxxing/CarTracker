# PHASE_17 — Real MOT History & Advisory Tracking

New phase. The user wants MOT tracking expanded well beyond the current status panel: pull the
vehicle's full MOT history (not just the latest test), extract every advisory/failure across all past
tests, and turn each into a trackable Planner item that gets crossed off once addressed — including
recognizing when the same issue recurs across multiple years (their example: tyres, already replaced,
so that item should already read as resolved once built).

This crosses a locked decision in `CLAUDE.md` ("Government data (DVLA/DVSA): mocked adapters only
until explicitly told to integrate real credentials"). Presented directly to the user via
`AskUserQuestion`: they chose to switch to real DVSA data now, matching their actual goal (advisories
describing their real car, not randomly-generated mock text). Two further UX decisions were also made
directly by the user rather than assumed: resolving an MOT-linked Planner item uses a lighter "mark
resolved" status, separate from the existing Idea→...→Done pipeline's auto-create-a-Service-Record
behavior; and advisories already resolved by the time of the first real import (the tyres) get handled
via a one-time manual cleanup pass after import, not automated fuzzy-matching against Service Records.

Full plan (grounding research, the six-increment approach, critical files, verification strategy) is
preserved in the approved plan at the time this phase started; increments below are documented as each
is actually completed, matching this project's established Phase 15/16 pattern. No browser/screenshot
tool exists in this environment — every increment gets build+test+curl verification from the agent,
real visual/functional review needs the user looking at it live.

## Increment 1: DVSA credential plumbing

### Task packet

```
TASK ID: PHASE-17-01
TITLE: DVSAConfig credential storage, following the existing MailConfig/OpenIDConfig pattern exactly
OBJECTIVE: Give the app a place to store DVSA MOT History API credentials (tenant id, client id,
  client secret, api key), configurable via the existing Setup UI, with zero adapter behavior change
  yet - this increment is pure plumbing.
INPUTS: Models/Configuration/MailConfig.cs (shape to mirror), Models/Settings/ServerConfig.cs
  (SMTPConfig/OIDCConfig nested-property pattern), Helper/ConfigHelper.cs (GetMailConfig/
  GetOpenIDConfig - confirmed both read live, per-call, via _config.GetSection("XConfig").
  Get<XConfig>() ?? new XConfig(), no caching/restart needed), Views/Home/Setup.cshtml + wwwroot/js/
  serversettings.js (the "Server Settings Configurator" wizard - confirmed this is the general
  settings editor, not a first-run-only flow), Enum/SkippedSetting.cs (the "Skip" toggle convention
  used by every other optional credential block on this page).
ALLOWED SCOPE: New DVSAConfig model; new ServerConfig.DVSAConfig nested property; new
  IConfigHelper.GetDVSAConfig(); new auto-null-when-empty guard in SaveServerConfig (matches the
  existing SMTP/OIDC guards); new ServerSettingsViewModel.DVSAConfig + HomeController.Setup() wiring;
  new UI fields + skip toggle on the existing "Miscellaneous" wizard page (page 6) rather than adding
  a whole new wizard page/renumbering, since Postgres/HTTPS/Kestrel-style rare admin-only settings
  already live there; new SkippedSetting.DVSA enum value; new serversettings.js DVSAConfig block +
  skip-null logic. No adapter, no DI change, no CLAUDE.md update yet (that's Increment 2).
NON-SCOPE: RealDVSAAdapter, Program.cs DI registration, DVSAMotComment.Dangerous field, any MOT
  history UI change, any Planner change.
IMPLEMENTATION REQUIREMENTS: Follow the MailConfig/OpenIDConfig pattern exactly - same
  [JsonPropertyName]/[JsonIgnore(Condition = WhenWritingNull)] attributes, same live-read config
  helper shape, same wizard-page skip-toggle markup, same JS setupData block shape.
DELIVERABLES: Models/Configuration/DVSAConfig.cs (new); Models/Settings/ServerConfig.cs; Helper/
  ConfigHelper.cs (interface + implementation + SaveServerConfig guard); Models/Settings/
  ServerSettingsViewModel.cs; Controllers/HomeController.cs (Setup action); Enum/SkippedSetting.cs;
  Views/Home/Setup.cshtml; wwwroot/js/serversettings.js.
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds with zero new warnings/errors.
  2. dotnet test passes (all pre-existing tests, no regressions).
  3. GET /setup renders the four new fields (Tenant Id, Client Id, Client Secret, API Key) plus a
     working Skip toggle under a "DVSA MOT History API" section on the Miscellaneous page.
  4. POST /Home/WriteServerConfiguration with DVSAConfig values persists them to
     data/config/serverConfig.json in the expected shape.
  5. With no DVSAConfig saved, GET /setup renders the fields empty ("Not Configured" placeholder),
     no exceptions - GetDVSAConfig() must return a non-null default object.
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj;
  dotnet run --urls http://localhost:5300 --no-build (dev port, not the production 5299 service);
  curl the /setup page and grep for the new field ids; curl-POST WriteServerConfiguration with
  throwaway test values and inspect data/config/serverConfig.json; reset the file back to {} afterward
  (throwaway test data, not real config).
STOP CONDITION: None hit.
```

### What was built

- `Models/Configuration/DVSAConfig.cs` — `TenantId`/`ClientId`/`ClientSecret`/`ApiKey`, all
  `string.Empty`-defaulted, mirroring `MailConfig.cs` exactly.
- `ServerConfig.DVSAConfig` nested nullable property, same `[JsonPropertyName("DVSAConfig")]` +
  `[JsonIgnore(WhenWritingNull)]` pattern as `SMTPConfig`/`OIDCConfig`.
- `IConfigHelper.GetDVSAConfig()` / `ConfigHelper.GetDVSAConfig()` — reads
  `_config.GetSection("DVSAConfig").Get<DVSAConfig>() ?? new DVSAConfig()`, live per-call like every
  other optional-integration config getter in this app (no caching, no restart needed after saving).
- `SaveServerConfig` auto-nullifies `DVSAConfig` when `ClientId` is blank, matching the existing
  SMTP(`EmailServer`)/OIDC(`Name`) empty-guard convention - so an unconfigured DVSA block never
  persists as a cluttered all-empty-strings object.
- `SkippedSetting.DVSA = 4` added for the Setup wizard's "Skip" checkbox round-trip bookkeeping (purely
  UI convenience - confirmed by reading `ConfigHelper.cs` that `SkippedSettings` drives no other server
  logic beyond re-checking the box next time the page loads).
- Four new fields + a "Skip" toggle added to the existing "Miscellaneous" wizard page
  (`Views/Home/Setup.cshtml`, `data-page="6"`) under a new "DVSA MOT History API" subsection -
  deliberately not a new wizard page, to avoid touching the nav bar/dropdown/button-visibility
  plumbing (`determineSetupButtons()`'s page-number switch) for what is a rare, one-time, admin-only
  settings block, consistent with how Postgres/HTTPS/Kestrel are already handled on this same page.
- `wwwroot/js/serversettings.js`'s `saveSetup()` now includes a `DVSAConfig` block in `setupData` and
  nulls it out + records the skip when `#skipDVSA` is checked, mirroring the SMTP/OIDC/Postgres/HTTPS
  blocks immediately above it.

### Verification

- `dotnet build CarCareTracker.csproj` — 0 warnings, 0 errors (after killing a stale dev-instance
  process from a prior session that was holding the build output locked).
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 10/10 passing, no regressions.
- Dev instance started on port 5300 (not 5299 - the production service). Confirmed via curl:
  - `GET /setup` renders `inputDVSATenantId`/`inputDVSAClientId`/`inputDVSAClientSecret`/
    `inputDVSAApiKey`/`skipDVSA` and the "DVSA MOT History API" section header.
  - `POST /Home/WriteServerConfiguration` with throwaway test credential values round-tripped
    correctly into `data/config/serverConfig.json` in the expected nested-object shape.
  - `data/config/serverConfig.json` reset to `{}` afterward (throwaway test data, not real
    configuration - the dev repo's config was empty before this test and is empty again after).
  - `GET /setup` with no `DVSAConfig` saved renders the fields empty with no exceptions, confirming
    `GetDVSAConfig()`'s non-null default works.
- Not yet deployed to production (`C:\Services\CarTracker`) - per this phase's verification strategy,
  production deployment happens once the full increment set (or a coherent subset) is ready for the
  user to actually configure real credentials against, not after every single plumbing-only increment.

## Increment 2: Real DVSA adapter, live-config-selected

### Task packet

```
TASK ID: PHASE-17-02
TITLE: RealDVSAAdapter - OAuth2 client-credentials + the real MOT History API call, transparent
  fallback to the existing mock when unconfigured
OBJECTIVE: Make IDVSAAdapter return real MOT history for vehicles once DVSA credentials are saved,
  with zero behavior change for everyone else (no credentials = exactly the same mock output as
  before). Confirm the exact current API endpoint against live docs rather than trusting prior
  (partially stale) research.
INPUTS: Increment 1's DVSAConfig/GetDVSAConfig(); External/Implementations/Mock/MockDVSAAdapter.cs
  (the fallback target, used as-is, unmodified); Models/GovernmentData/DVSAMotHistory.cs (the model
  to extend + reuse directly as the API response DTO); Program.cs:117-118 (DI registration);
  Controllers/Vehicle/ReportController.cs + Controllers/API/GovernmentDataController.cs (the two
  IDVSAAdapter.GetMotHistory call sites - both synchronous MVC actions, confirming the interface
  should stay synchronous rather than rippling an async change through them).
ALLOWED SCOPE: New RealDVSAAdapter class (OAuth2 token fetch/cache/refresh + the real API call);
  DVSAMotComment.Dangerous field; Program.cs DI registration swap (MockDVSAAdapter -> RealDVSAAdapter
  as the one registered IDVSAAdapter); CLAUDE.md locked-decision bullet update recording the user's
  sign-off. No UI change (Increment 3), no Planner change.
NON-SCOPE: Any change to the two call sites' signatures/behavior beyond what falls out naturally from
  IDVSAAdapter's contract staying identical; MOT history UI; Planner linkage.
IMPLEMENTATION REQUIREMENTS:
  - Endpoint reconfirmed live (not from prior possibly-stale research) via WebFetch/WebSearch against
    documentation.history.mot.api.gov.uk: GET https://history.mot.api.gov.uk/v1/trade/vehicles/
    registration/{registration}, headers Authorization: Bearer {token} + x-api-key: {key}. Token via
    POST https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token (grant_type=client_credentials,
    client_id, client_secret, scope=https://tapi.dvsa.gov.uk/.default), cached in-memory and refreshed
    a minute before its ~1200s expiry.
  - Use HttpClient.Send (genuinely synchronous since .NET 5, not GetAwaiter().GetResult() blocking-on-
    async) to keep IDVSAAdapter's interface synchronous, consistent with every other adapter in
    External/Interfaces (none of which are async) and both real call sites.
  - DVSAMotHistory's shape already mirrors the real API's field names closely enough
    (registration/make/model/firstUsedDate/fuelType/primaryColour/motTests[.../rfrAndComments[text/
    type/dangerous]]) that the real API's JSON response deserializes directly into the existing model
    with PropertyNameCaseInsensitive=true - no separate response DTO needed, keeping the model as the
    single source of truth for the shape as its doc-comment already claimed.
  - Not-configured (any of the 4 DVSAConfig fields blank) and any exception during the real call both
    fall back / degrade gracefully - the former to MockDVSAAdapter's output unchanged, the latter to a
    Found=false result with the error logged via ILogger, never an unhandled exception reaching the
    controller.
DELIVERABLES: External/Implementations/Real/RealDVSAAdapter.cs (new); Models/GovernmentData/
  DVSAMotHistory.cs; Program.cs; CLAUDE.md; Tests/DVSAAdapterFallbackTests.cs (new).
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds, 0 new warnings/errors.
  2. dotnet test passes, including a new regression test proving the unconfigured-fallback path.
  3. curl against /api/vehicle/governmentdata for both real dev vehicles (BMW Z4 id=1, Volvo S80 id=2)
     with no DVSAConfig saved returns identical mock-backed output to before this increment
     (isMockData:true, full multi-test history, the new dangerous field present and false).
  4. curl with fake-but-complete DVSAConfig credentials saved proves the real code path actually runs
     (a genuine request reaches login.microsoftonline.com and gets a real 400 from Microsoft, not a
     local validation short-circuit) and fails gracefully (found:false, isMockData:false, error
     logged, HTTP 200 still returned to the client - no crash).
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj;
  dotnet run --urls http://localhost:5300 --no-build; curl the governmentdata endpoint before/during/
  after a throwaway DVSAConfig POST, inspecting both the response and the server log; reset
  serverConfig.json to {} afterward.
STOP CONDITION: None hit - the only open question (exact endpoint path) was resolved by live-doc
  lookup before implementation, not by guessing.
```

### What was built

- `External/Implementations/Real/RealDVSAAdapter.cs` — the single registered `IDVSAAdapter`. Reads
  `GetDVSAConfig()` on every call (live, no restart needed); if any of the four fields is blank,
  delegates straight to an internal `MockDVSAAdapter` instance, byte-for-byte the same behavior as
  before this increment. If configured, fetches/caches an OAuth2 bearer token (Entra ID client-
  credentials flow, refreshed ~1 minute before its real expiry) and calls the real MOT History API,
  deserializing its response directly into the existing `DVSAMotHistory` model
  (`PropertyNameCaseInsensitive`, no separate DTO). A 404 from the API or a blank registration both map
  to `Found = false`; any other failure (bad credentials, network error, unexpected shape) is caught,
  logged via `ILogger<RealDVSAAdapter>`, and also degrades to `Found = false` rather than throwing.
- `DVSAMotComment.Dangerous` (bool) added — the one real field gap between the existing model and the
  live API's `rfrAndComments` objects, confirmed via WebFetch against
  `documentation.history.mot.api.gov.uk`.
- `Program.cs`: `IDVSAAdapter` now resolves to `RealDVSAAdapter` (was `MockDVSAAdapter` directly);
  `IDVLAAdapter` untouched (still explicitly mocked-only, unaffected by this increment).
- `CLAUDE.md`'s "Government data" locked-decision bullet and the matching MUST-NOT line updated to
  record the user's sign-off for DVSA specifically - DVLA is explicitly called out as still requiring
  its own separate sign-off if that's ever revisited.
- `Tests/DVSAAdapterFallbackTests.cs` (new) — an integration test (matching this project's established
  `WebApplicationFactory` style, no mocking library exists in this test project) proving the
  unconfigured-fallback path through the real DI-wired adapter, not just in isolation.

### Verification

- `dotnet build CarCareTracker.csproj` — 0 warnings, 0 errors (after killing a leftover dev-instance
  process from Increment 1's session holding the build lock, same as before - not a new issue).
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 11/11 passing (10 pre-existing + 1 new), no
  regressions.
- Dev instance on port 5300. Curl-verified against both real dev vehicles (BMW Z4 id=1, Volvo S80
  id=2): with no `DVSAConfig` saved, `/api/vehicle/governmentdata` returns identical mock-backed
  output to before this increment (full multi-test history, `isMockData:true`, the new `dangerous`
  field present on every comment, defaulting to `false`).
- **Real end-to-end failure-path proof**: saved fake-but-complete `DVSAConfig` credentials via
  `WriteServerConfiguration`, waited ~2s for the config file watcher's reload debounce (a real, minor,
  expected delay - not a bug), then confirmed via the server log that `RealDVSAAdapter` genuinely
  reached `login.microsoftonline.com` and received an actual HTTP 400 from Microsoft's real OAuth
  endpoint (proving the request was correctly formed and actually sent, not just locally validated),
  caught the resulting exception, logged it clearly, and returned a well-formed `Found:false` result
  with HTTP 200 still returned to the client - no crash, no unhandled exception. `serverConfig.json`
  reset to `{}` afterward (throwaway test data).
- Not yet deployed to production - same reasoning as Increment 1 (no user-visible behavior change
  without real credentials, which the user hasn't registered for yet).

## Increment 3: Full MOT history UI

### Task packet

```
TASK ID: PHASE-17-03
TITLE: Show every past MOT test (not just the latest), colour-coded by advisory/failure severity;
  fix a pre-existing Mock-badge bug found during Plan review
OBJECTIVE: Stop discarding MotHistory.MotTests down to a single .FirstOrDefault() - the full history
  already exists in the data model and adapter output (confirmed since Phase 8's mock), the only real
  gap was the view. Also fix the "Mock" badge checking DVLAData.Found (always true once a plate is
  set) instead of IsMockData (now meaningfully different per-source since Increment 2).
INPUTS: Views/Vehicle/Report/_GovernmentData.cshtml (the only file needing structural change);
  wwwroot/css/site.css (confirmed status-badge-success/warning/danger/neutral all exist - no new CSS
  needed); Views/Vehicle/Report/_Report.cshtml (confirmed the only view including this partial, no JS
  anywhere depends on its internal structure - safe to restructure freely).
ALLOWED SCOPE: _GovernmentData.cshtml only. No controller/model change - VehicleGovernmentDataViewModel
  already carries the full MotTests list, just wasn't being rendered.
NON-SCOPE: Planner linkage, recurring-advisory grouping (Increments 4-5), resolved status
  (Increment 6).
IMPLEMENTATION REQUIREMENTS:
  - Render every test in Model.MotHistory.MotTests (newest first), not just .FirstOrDefault().
  - Each test's overall result badge (PASSED/FAILED) unchanged in meaning; each rfrAndComments entry
    gets its own badge for its Type (DANGEROUS/FAIL/MAJOR -> danger, ADVISORY/MINOR -> warning,
    anything else -> neutral) plus its text, replacing the old plain-text-only list.
  - Split the single top-of-card "Mock" badge into two independent ones: the existing one now checks
    DVLAData.IsMockData (DVLA stays permanently mocked, so this will always show for now, but is now
    correct by construction rather than by coincidence); a new one next to a "MOT History" sub-header
    checks MotHistory.IsMockData specifically, since MOT can now be real while DVLA stays mocked -
    users need to know which specific data source they're looking at.
  - Gate the whole MOT History section on Model.MotHistory.Found (not just "list happens to be non-
    empty") so a real API 404/failure (Increment 2's graceful-degradation path) shows nothing rather
    than a misleading empty section.
DELIVERABLES: Views/Vehicle/Report/_GovernmentData.cshtml.
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds, 0 new warnings/errors.
  2. dotnet test passes, no regressions.
  3. curl against /Vehicle/GetReportPartialView for both real dev vehicles shows ALL past tests (4 for
     the BMW, 4 for the Volvo), not just the latest, each correctly colour-coded, with tests that have
     no advisories correctly showing no orphan empty list.
  4. Both the DVLA-level and MOT-History-level Mock badges render independently and correctly.
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj;
  dotnet run --urls http://localhost:5300 --no-build; curl /Vehicle/GetReportPartialView?vehicleId=1
  and ?vehicleId=2, grep the rendered HTML for the expected badges/test count.
STOP CONDITION: None hit.
```

### What was built

- `Views/Vehicle/Report/_GovernmentData.cshtml` rewritten: `Model.MotHistory.MotTests
  .OrderByDescending(x => x.CompletedDate)` (no more `.FirstOrDefault()`) renders every past test, each
  in its own block with its PASSED/FAILED badge, date/odometer line, and a colour-coded badge per
  advisory/failure comment (`GetCommentBadgeClass` - danger for DANGEROUS/FAIL/MAJOR/`comment.Dangerous`,
  warning for ADVISORY/MINOR, neutral otherwise) followed by the comment text.
- The single "Mock" badge is now two independent ones: the original (top of card) checks
  `DVLAData.IsMockData` instead of the previous `DVLAData.Found` bug; a new one sits beside a "MOT
  History" sub-header and checks `MotHistory.IsMockData` specifically - meaningful now that MOT can be
  real while DVLA stays permanently mocked.
- The whole MOT History section is gated on `Model.MotHistory.Found` (not just a non-empty list), so
  a real API failure (Increment 2's `Found:false` graceful-degradation path) correctly shows nothing
  rather than an empty, misleadingly-present section.

### Verification

- `dotnet build CarCareTracker.csproj` — 0 new warnings, 0 errors.
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 11/11 passing, no regressions.
- Dev instance on port 5300. Curl-verified `/Vehicle/GetReportPartialView` for both real dev vehicles:
  - BMW Z4 (id=1): all 4 past tests render (2023 PASSED/ADVISORY, 2024 FAILED/MAJOR, 2025 PASSED/
    ADVISORY, 2026 FAILED/MAJOR), newest first, each badge correctly coloured (danger for
    FAILED/MAJOR, warning for ADVISORY, success for PASSED), both Mock badges present.
  - Volvo S80 (id=2): all 4 past tests render (2021-2024, three with zero advisories correctly showing
    no `<ul>` at all, one 2024 entry with a single ADVISORY badge+text), confirming the empty-comments
    case renders cleanly with no orphan markup.
- Not yet deployed to production - same reasoning as Increments 1-2 (no real-credential behavior
  change yet visible to the user; this increment is a pure display improvement on top of the same mock
  data they've already seen).

## Increment 4: Advisory text normalization

### Task packet

```
TASK ID: PHASE-17-04
TITLE: Normalize MOT advisory text into a stable per-vehicle dedup key, before anything creates
  Planner items from advisories
OBJECTIVE: Give later increments a single, correct place to decide "is this the same real-world issue
  as one flagged in an earlier test." A Plan-agent design review (run before Increment 1 started)
  explicitly flagged the risk of building this AFTER the Planner-linkage actions (original plan order):
  if "Add to Planner" ships first keyed by raw per-test comment text, a 3-year-recurring "front tyre
  worn" advisory becomes 3 separate cards, and fixing the key format later is a breaking change for
  already-created records. This increment closes that gap before it can happen.
INPUTS: External/Implementations/Mock/MockDVSAAdapter.cs's AdvisoryPhrases (realistic example text,
  no reference codes though - real DVSA text does include trailing codes like "(5.2.3)" per the API
  docs read in Increment 2's research); Helper/StaticHelper.cs (confirmed the right home - a large
  collection of pure, stateless static utility functions with an existing precedent method,
  GetHash(string), reused directly rather than inventing a new hashing approach); PlanRecord.VehicleId
  (confirmed the key must be scoped per-vehicle - the same wording on two different vehicles must not
  collapse into one Planner item).
ALLOWED SCOPE: Two new static functions on StaticHelper (NormalizeMotAdvisoryText,
  GetMotAdvisoryKey(vehicleId, text)) + unit tests. No PlanRecord field yet, no UI, no controller
  action - Increment 5 consumes this.
NON-SCOPE: PlanRecord.SourceMotKey field, "Add to Planner" actions, recurring-advisory UI grouping
  (all Increment 5).
IMPLEMENTATION REQUIREMENTS: Strip a trailing parenthetical reference code, lowercase, collapse
  internal whitespace - matching the plan's stated heuristic exactly (not a guarantee against every
  possible DVSA re-wording, an explicitly acknowledged known limitation for a single-user personal app,
  not over-built with fuzzy NLP matching).
DELIVERABLES: Helper/StaticHelper.cs; Tests/MotAdvisoryNormalizationTests.cs (new).
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds, 0 new warnings/errors.
  2. dotnet test passes, including new unit tests proving: trailing reference codes are stripped;
     case/whitespace differences collapse to the same key; genuinely different advisory text stays
     different; blank input is handled; the same normalized text on two different vehicles produces
     different keys (per-vehicle scoping actually works).
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj.
STOP CONDITION: None hit.
```

### What was built

- `StaticHelper.NormalizeMotAdvisoryText(string text)` — strips a trailing `(...)` reference code,
  lowercases, collapses internal whitespace; blank/whitespace-only input returns `string.Empty`.
- `StaticHelper.GetMotAdvisoryKey(int vehicleId, string text)` — combines the vehicle id with the
  normalized text and reuses the existing `GetHash` (SHA-256 hex) rather than inventing a new hashing
  approach, so the same real-world issue recurring across multiple years of one vehicle's MOT tests
  produces the same key, while the identical wording on a different vehicle does not.
- `Tests/MotAdvisoryNormalizationTests.cs` (new) — 6 pure unit tests (no `WebApplicationFactory`
  needed, since these are dependency-free functions) covering reference-code stripping, case/whitespace
  insensitivity, distinct-text distinctness, blank input, and per-vehicle key scoping in both
  directions (same text+vehicle -> same key; same text, different vehicle -> different key).

### Verification

- `dotnet build CarCareTracker.csproj` — 0 new warnings, 0 errors.
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 17/17 passing (11 pre-existing + 6 new), no
  regressions.
- No curl/UI verification needed - this increment has no HTTP surface yet, by design (Increment 5
  consumes it).

## Increment 5: Advisory -> Planner linkage

### Task packet

```
TASK ID: PHASE-17-05
TITLE: "Add to Planner" (single + bulk) actions, recurring-advisory grouping in the MOT history view
OBJECTIVE: Let the user turn an MOT advisory into a tracked Planner item with one click - a single
  advisory, or the whole vehicle's open advisories in bulk - deduped by StaticHelper.GetMotAdvisoryKey
  so a recurring issue (the user's tyres example) collapses into exactly one Planner item regardless of
  how many tests flagged it, and re-running import/add never creates duplicates.
INPUTS: Increment 4's StaticHelper.GetMotAdvisoryKey; PlanRecord.cs/PlanRecordInput.cs/
  _PlanRecordModal.cshtml/planrecord.js (the ReminderRecordId round-trip mechanism - confirmed by
  reading the actual save flow that it's a JS-tracked value captured from the modal's initial
  server-rendered state via getPlanRecordModelData(), re-submitted on every save, never a directly
  user-editable form field - SourceMotKey follows this exact mechanism, not a full exclusion from
  PlanRecordInput as originally summarized); Controllers/Vehicle/PlanController.cs's
  SavePlanRecordToVehicleId (the pattern to mirror for the two new actions - UserCanEditVehicle
  security check, _planRecordDataAccess.SavePlanRecordToVehicle, WebHookPayload.FromPlanRecord event);
  IPlanRecordDataAccess.GetPlanRecordsByVehicleId (already sufficient for in-memory dedup checks - no
  new data-access method needed, this is a personal app with a handful of Planner items per vehicle,
  not thousands).
ALLOWED SCOPE: PlanRecord.SourceMotKey field; PlanRecordInput round-trip (input field, ToPlanRecord(),
  GetPlanRecordForEditById's reverse mapping, modal JS, planrecord.js); two new PlanController actions
  (AddMotAdvisoryToPlanner, ImportAllMotAdvisoriesToPlanner); VehicleGovernmentDataViewModel.VehicleId
  + ExistingMotPlanKeys; both controllers populating VehicleGovernmentDataViewModel
  (ReportController.GetReportPartialView, GovernmentDataController.GetGovernmentDataForVehicle);
  _GovernmentData.cshtml (grouped "Advisories & Failures" section + bulk import button); new
  reports.js functions.
NON-SCOPE: The lighter "mark resolved" status and its one-time cleanup pass (Increment 6).
IMPLEMENTATION REQUIREMENTS:
  - One advisory = one PlanRecord (confirmed correct granularity by the earlier Plan-agent review -
    bundling multiple advisories per test would force all-or-nothing resolution later).
  - SourceMotKey must never be settable through the free-text edit form - only ever set by the new
    "Add to Planner"/bulk-import actions, then preserved (not reset to empty) on ordinary subsequent
    edits via the same JS-variable round-trip ReminderRecordId already uses.
  - Real edge case found and fixed while wiring the round-trip: "Save as Template" reuses the same
    getAndValidatePlanRecordValues() payload, which would have carried a stale SourceMotKey onto a
    reusable template - every future record created from that template would incorrectly appear
    already-linked to an advisory it has nothing to do with. Fixed by clearing SourceMotKey
    server-side in SavePlanRecordTemplateToVehicleId before persisting the template.
  - Recurring advisories must collapse in the UI too, not just in the dedup key: group all
    RfrAndComments across a vehicle's entire MOT history by GetMotAdvisoryKey, show the worst-severity
    badge across occurrences plus which years it was flagged, one "Add to Planner"/"Added" state per
    group - not one row (and one confusing partially-rejected button click) per raw per-test occurrence.
  - Text passed from the view into the "Add to Planner" action is carried via a data-* attribute (jQuery
    .data(), HTML-attribute-encoded by Razor automatically) rather than inlined into the onclick JS call
    - avoids manual JS-string-escaping of arbitrary advisory text entirely.
DELIVERABLES: Models/PlanRecord/PlanRecord.cs, PlanRecordInput.cs; Controllers/Vehicle/PlanController.cs;
  Controllers/Vehicle/ReportController.cs; Controllers/API/GovernmentDataController.cs; Models/
  GovernmentData/VehicleGovernmentDataViewModel.cs; Views/Vehicle/Plan/_PlanRecordModal.cshtml;
  Views/Vehicle/Report/_GovernmentData.cshtml; wwwroot/js/planrecord.js; wwwroot/js/reports.js;
  Tests/MotAdvisoryPlannerLinkageTests.cs (new).
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds, 0 new warnings/errors.
  2. dotnet test passes, including new integration tests proving: adding the same advisory twice is
     rejected the second time (exactly 1 PlanRecord exists); bulk import skips an already-added
     advisory and only adds the remaining unique ones (correct addedCount); a second bulk import call
     adds zero (full idempotency).
  3. curl against a real dev vehicle (BMW Z4 id=1, which has two advisories each recurring across two
     of its four mock tests) confirms: the grouped section shows exactly 2 rows (not 4), each with the
     correct "(flagged: year, year)" list and worst-severity badge; clicking Add/Import correctly
     flips the row to an "Added" badge and removes the button; deleting the created PlanRecords
     correctly makes the buttons reappear (round-trip proven in both directions).
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj;
  dotnet run --urls http://localhost:5300 --no-build; curl the report partial and the two new POST
  actions against vehicleId=1, then clean up any created PlanRecords via DeletePlanRecordById.
STOP CONDITION: None hit.
```

### What was built

- `PlanRecord.SourceMotKey` (new field) + the full round-trip through `PlanRecordInput`
  (`ToPlanRecord()`, `GetPlanRecordForEditById`'s reverse mapping, `_PlanRecordModal.cshtml`'s
  `getPlanRecordModelData()`, `planrecord.js`'s `getAndValidatePlanRecordValues()`) - mirrors
  `ReminderRecordId`'s exact existing mechanism (a JS-tracked value invisible to the user but preserved
  across ordinary saves), not a full exclusion as first summarized.
- **Real bug caught and fixed while wiring this**: `SavePlanRecordTemplateToVehicleId` now explicitly
  clears `SourceMotKey` before persisting a template, since "Save as Template" reuses the same payload
  builder and would otherwise have let a template silently carry a stale advisory link forward onto
  every record later created from it.
- `PlanController.AddMotAdvisoryToPlanner(vehicleId, advisoryText)` - creates one `PlanRecord`
  (`ImportMode.ServiceRecord`, `PlanPriority.Normal`, `PlanProgress.Idea`) keyed by
  `GetMotAdvisoryKey(vehicleId, advisoryText)`, rejecting the call if that key already has a linked
  record for this vehicle.
- `PlanController.ImportAllMotAdvisoriesToPlanner(vehicleId)` - iterates every comment across the
  vehicle's entire real (or mock) MOT history, creates one `PlanRecord` per unique key not already
  present (checked against both existing records and keys already added earlier in the same loop),
  returns the count actually added.
- `VehicleGovernmentDataViewModel.VehicleId` + `ExistingMotPlanKeys` - both government-data-serving
  controllers (`ReportController.GetReportPartialView`, `GovernmentDataController.
  GetGovernmentDataForVehicle`) now also query existing `PlanRecord.SourceMotKey`s so the view can
  render "Added" without a second round trip.
- `_GovernmentData.cshtml`: a new "Advisories & Failures" section, grouped by `GetMotAdvisoryKey` across
  the vehicle's whole MOT history (not per-test), each row showing the worst-severity badge seen, the
  years it was flagged (e.g. "flagged: 2023, 2025"), and either an "Add to Planner" button or an
  "Added" badge - plus a bulk "Import All to Planner" button. The existing per-test chronological list
  from Increment 3 is unchanged below it. Advisory text reaches the JS handlers via `data-*` attributes
  (Razor's automatic HTML-attribute encoding), not inlined into `onclick`, avoiding manual JS-string
  escaping of arbitrary text.
- `reports.js`: `addMotAdvisoryToPlanner`/`importAllMotAdvisoriesToPlanner`, both refreshing the report
  panel via the existing `getVehicleReport(vehicleId)` on success.
- `Tests/MotAdvisoryPlannerLinkageTests.cs` (new) - 2 integration tests against a deliberately-chosen
  deterministic mock plate ("MOTPLAN003", 3 distinct advisories) proving single-add dedup and bulk-import
  dedup/idempotency end-to-end through the real DI-wired controllers.

### Verification

- `dotnet build CarCareTracker.csproj` — 0 new warnings, 0 errors.
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 19/19 passing (17 pre-existing + 2 new), no
  regressions.
- Dev instance on port 5300. Curl-verified against the real BMW Z4 (vehicleId=1, whose mock data has
  "Nearside front brake pad(s) worn below 1.5mm" recurring in 2024+2026 and "Nearside headlamp aim
  slightly high" recurring in 2023+2025): the grouped section correctly showed exactly 2 rows (not 4),
  each with the correct "(flagged: ...)" year list and severity badge - **direct proof of the exact
  recurring-advisory-collapse behavior the user originally asked for** (their tyres example). Added one
  advisory via the single-add action, bulk-imported the other (`addedCount:1`, correctly skipping the
  already-added one), confirmed both rows flipped to "Added" badges with buttons removed, then deleted
  both created `PlanRecord`s and confirmed the buttons correctly reappeared - the round-trip verified in
  both directions against real data, not just the throwaway test plate. Dev environment restored to its
  pre-verification state afterward.
- Not yet deployed to production - this is the first increment with real new user-facing functionality
  the user would actually want to use; deployment is planned once Increment 6 (the "mark resolved"
  status and cleanup pass) completes the full loop the user asked for, so there's one coherent thing to
  review rather than a half-finished feature.

## Increment 6: Lighter "mark resolved" status

### Task packet

```
TASK ID: PHASE-17-06
TITLE: Orthogonal "mark resolved" status for MOT-linked Planner items - the final increment
OBJECTIVE: Let the user cross off an MOT advisory once it's actually been addressed, without routing
  through PlanProgress.Done (which auto-creates a ServiceRecord - the wrong behavior when the real fix
  was already logged separately, or wasn't a "service" at all). Also close the loop on the user's
  original ask: their tyres, already replaced in real life, need a one-time way to be marked resolved
  the moment this feature exists, not treated as new open work.
INPUTS: The earlier Plan-agent review's finding that PlanProgress is load-bearing in exactly 6
  hardcoded Kanban swimlanes (Views/Vehicle/Plan/_PlanRecords.cshtml) and the API's validation
  (Controllers/API/PlanController.cs) - confirms a 7th enum value isn't viable, must be an orthogonal
  field; Increment 5's SourceMotKey round-trip mechanism (ReminderRecordId-style: JS-tracked, never a
  raw form field, preserved across ordinary edits) as the pattern to repeat for ResolvedDate;
  Views/Vehicle/Plan/_PlanRecordItem.cshtml (the Kanban card partial - confirmed the existing Done
  styling precedent: <s>strikethrough</s> + a non-drag/click-to-delete behavior that must NOT be
  copied for resolved-but-not-Done items, since resolving must stay independent of Progress entirely).
ALLOWED SCOPE: PlanRecord.ResolvedDate (nullable DateTime); the same round-trip plumbing as
  SourceMotKey (PlanRecordInput, modal JS, GetPlanRecordForEditById, template-save clearing); two new
  PlanController actions (MarkPlanRecordResolved/UnmarkPlanRecordResolved); _PlanRecordItem.cshtml
  (strikethrough independent of Progress + a Mark Resolved/Resolved toggle scoped to MOT-linked cards
  only, i.e. non-empty SourceMotKey); new planrecord.js functions.
NON-SCOPE: A dedicated separate "cleanup pass" modal/checklist UI - deliberately not built (see below).
IMPLEMENTATION REQUIREMENTS:
  - ResolvedDate must NOT change Progress or trigger any Done-pipeline side effect - verified directly
    (see below) rather than just asserted.
  - The toggle button lives on the card itself (not the shared context-menu system, which only receives
    planRecordId/currentSwimLane today and would need broader changes to also carry SourceMotKey/
    ResolvedDate per-card) using event.stopPropagation() so it doesn't also trigger the card's own
    click-to-edit handler - a simpler, more isolated change than extending the shared menu.
  - "One-time manual cleanup pass" is satisfied by the Kanban board itself, not a separate UI: after a
    bulk import, every newly-created MOT-linked card already carries a "Mark Resolved" toggle right on
    it, in the same board the user already reviews - reusing the existing surface rather than building
    a parallel checklist modal for what is fundamentally the same review task.
DELIVERABLES: Models/PlanRecord/PlanRecord.cs, PlanRecordInput.cs; Controllers/Vehicle/PlanController.cs;
  Views/Vehicle/Plan/_PlanRecordModal.cshtml, _PlanRecordItem.cshtml; wwwroot/js/planrecord.js;
  Tests/PlanRecordResolvedStatusTests.cs (new); docs/execution/DEFERRED.md (a real pre-existing bug
  found along the way, logged rather than silently fixed out-of-scope - see below).
ACCEPTANCE CRITERIA:
  1. dotnet build succeeds, 0 new warnings/errors.
  2. dotnet test passes, including new tests proving: mark then unmark both succeed; marking an
     already-resolved item again is harmless (not an error); a nonexistent planRecordId fails
     gracefully (not a 500 - this caught a real bug, see below); resolving never changes Progress.
  3. curl against a real dev vehicle's MOT-linked PlanRecord confirms, in order: the card renders with
     no strikethrough and a "Mark Resolved" button beforehand; marking resolved adds `<s>` strikethrough
     and flips the button to a "Resolved" badge while the card's onclick/draggable/context-menu swim
     lane stay exactly as before (still "Idea", still opens the edit modal, still draggable); the edit
     modal shows a non-empty resolvedDate; **saving the record through the ordinary edit-and-save flow
     with an unrelated field changed (cost 0 -> 25) preserves resolvedDate unchanged** - the actual
     round-trip risk this mechanism exists to prevent; deleting the record cleans up correctly.
VALIDATION COMMANDS: dotnet build CarCareTracker.csproj; dotnet test Tests/CarCareTracker.Tests.csproj;
  dotnet run --urls http://localhost:5300 --no-build; curl through the full add -> resolve -> ordinary-
  edit -> verify-preserved -> delete sequence against vehicleId=1's real MOT data.
STOP CONDITION: None hit.
```

### What was built

- `PlanRecord.ResolvedDate` (nullable `DateTime`) + the full round-trip through `PlanRecordInput`
  (string-typed on the input, matching `DateCreated`/`DateModified`'s existing convention),
  `GetPlanRecordForEditById`, `_PlanRecordModal.cshtml`'s `getPlanRecordModelData()`, and
  `planrecord.js`'s `getAndValidatePlanRecordValues()` - the exact same JS-tracked, never-a-raw-form-
  field mechanism as `SourceMotKey`/`ReminderRecordId`.
- `SavePlanRecordTemplateToVehicleId` now also clears `ResolvedDate` (alongside the `SourceMotKey`
  clear from Increment 5) before persisting a template, for the same reason.
- `PlanController.MarkPlanRecordResolved(planRecordId)` / `UnmarkPlanRecordResolved(planRecordId)` -
  set/clear `ResolvedDate` directly on the existing record via `SavePlanRecordToVehicle`, touching
  nothing about `Progress`.
- **Real bug found and fixed while testing**: both new actions initially copied the existing
  `GetPlanRecordById(id).Id == default` pattern used elsewhere in this controller
  (`DeletePlanRecordById`, `GetPlanRecordForEditById`) to detect a missing record - but the LiteDB
  implementation's `table.FindById()` returns `null`, not a default empty object, for a nonexistent id,
  so this pattern NullReferenceExceptions (500) instead of failing gracefully. Caught by
  `MarkResolved_NonexistentPlanRecord_Fails` actually failing on first run - fixed in both new actions
  with an explicit `existingRecord == null` guard. The two pre-existing call sites carrying the same
  latent bug were left untouched (out of this increment's scope) and logged in `DEFERRED.md` instead
  of silently fixed, so it isn't lost.
- `_PlanRecordItem.cshtml`: strikethrough now applies when `Progress == Done` **or** `ResolvedDate.
  HasValue`, independent of each other. For cards with a non-empty `SourceMotKey`, a badge/toggle shows
  "Mark Resolved" (neutral) or "Resolved" (success, checkmark) with `event.stopPropagation()` so
  clicking it doesn't also open the edit modal - deliberately built on the card itself rather than
  extending the shared right-click context-menu system, which doesn't currently carry per-card
  `SourceMotKey`/`ResolvedDate` data.
- `planrecord.js`: `markPlanRecordResolved`/`unmarkPlanRecordResolved`, both refreshing the board via
  the existing `getVehiclePlanRecords(vehicleId)` on success.
- `Tests/PlanRecordResolvedStatusTests.cs` (new) - 3 integration tests: mark/unmark both succeed and
  marking twice is harmless; a nonexistent id fails gracefully; resolving leaves `Progress` unchanged
  (still "Idea").
- `docs/execution/DEFERRED.md`: logged the pre-existing null-safety gap in `DeletePlanRecordById`/
  `GetPlanRecordForEditById` found while fixing the same pattern in this increment's own new code.

### Verification

- `dotnet build CarCareTracker.csproj` — 0 new warnings, 0 errors.
- `dotnet test Tests/CarCareTracker.Tests.csproj` — 22/22 passing (19 pre-existing + 3 new; one of the
  3 failed on first run against real HTTP 500s before the null-check fix, then passed after - a genuine
  bug caught by the test, not a test written to match already-correct behavior).
- Dev instance on port 5300. Full curl sequence against the real BMW Z4 (vehicleId=1): added an
  advisory, confirmed the Kanban card rendered with no strikethrough and a "Mark Resolved" button;
  marked it resolved and confirmed strikethrough + "Resolved" badge appeared while `onclick`/
  `draggable`/the context-menu's swim-lane argument (still `'Idea'`) stayed completely unchanged -
  direct proof resolving never touches `Progress`; confirmed the edit modal's `resolvedDate` JS
  variable was populated; **saved the record through the ordinary edit-and-save flow with its cost
  changed from 0 to 25, and confirmed `resolvedDate` survived unchanged** - the specific round-trip
  failure mode this mechanism exists to prevent, directly exercised and proven fixed; deleted the
  record and confirmed cleanup. Dev environment restored to its pre-verification state afterward.
- **This is the last increment of Phase 17.** The full loop the user asked for now works end-to-end:
  real (or mock-fallback) MOT history -> every past test visible -> recurring advisories grouped
  across years -> one-click add to Planner (single or bulk) -> mark resolved once addressed, all
  without disturbing the existing Done/ServiceRecord pipeline. Production deployment is the next step,
  not yet done - nothing in Phase 17 has reached the user's phone/production service yet.
