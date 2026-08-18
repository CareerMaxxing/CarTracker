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
