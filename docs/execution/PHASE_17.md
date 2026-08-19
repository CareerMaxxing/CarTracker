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
