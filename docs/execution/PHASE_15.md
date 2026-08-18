# PHASE_15 — Remote Access & Persistent Hosting

New phase, distinct from Phase 14's "V1 Hardening" objective (tests/accessibility/performance/
security review of existing functionality). This phase makes the already-complete app reachable
from the user's phone with the same live data as the PC, over a private Tailscale network - not a
new sync architecture, since the app is already a single server with SignalR already broadcasting
live updates to every connected client. This intentionally revisits two of `CLAUDE.md`'s locked
decisions ("Runtime: local PC / localhost only. No cloud deployment." and "Offline/sync: local-first,
no cloud sync ... unless a later requirement explicitly demands it.") - the human owner explicitly
authorized this in conversation and chose Tailscale specifically over LAN-only access or public
internet hosting, keeping the app running only on the home PC, reachable only from devices added to
the user's own private tailnet, never exposed to the public internet. AI/OCR invoice scanning was
explicitly named by the user as a later, separate addition - not addressed by this phase.

Full plan (all four increments, considered alternatives, and why Docker was rejected in favor of a
Windows Service) is preserved in the approved plan at the time this phase started; increments below
are documented as each is actually completed, matching this project's established Phase 14 pattern.

## Increment 1: Windows Service hosting readiness

### Task packet

```
TASK ID: PHASE-15-01
TITLE: Make the app safe to run as a persistent Windows Service
OBJECTIVE: Fix the two things that would silently break if this app were registered as a Windows
  Service today, without changing any behavior for the existing interactive dotnet run workflow.
INPUTS: Program.cs, CarCareTracker.csproj, Helper/StaticHelper.cs (DbName/UserConfigPath/
  ServerConfigPath - confirmed all CWD-relative literal constants, e.g. "data/cartracker.db").
ALLOWED SCOPE: A new NuGet package reference; a startup-order-sensitive working-directory fix
  scoped to only the Windows Service case; a DataProtection key-persistence change.
NON-SCOPE: Actually publishing/registering/starting the service (Increment 2 - requires admin
  elevation on the user's machine, not something to do without them present); Kestrel binding
  configuration (already exists as a working Setup UI, no code needed, covered in Increment 2);
  Tailscale installation (user-side, Increment 2); enabling auth (Increment 3).
IMPLEMENTATION REQUIREMENTS:
  - Add Microsoft.Extensions.Hosting.WindowsServices; call builder.Host.UseWindowsService() early
    in Program.cs.
  - Real gap found during investigation, not in the original plan: every data path in this app
    (StaticHelper.DbName = "data/cartracker.db", UserConfigPath, ServerConfigPath) is a CWD-relative
    literal, resolved against the process's current directory - not ContentRootPath. The Windows
    Service Control Manager launches services with a default working directory of System32, not the
    published app's own folder. Left unfixed, a Windows Service deployment would silently create/
    look for data/ under C:\Windows\System32\data\ instead of the app folder. Fixed by calling
    Directory.SetCurrentDirectory(AppContext.BaseDirectory) at the very top of Program.cs, before any
    CWD-relative logic runs (StaticHelper.CheckMigration, the config JSON file additions) - but only
    when WindowsServiceHelpers.IsWindowsService() is true, so the existing interactive dotnet run
    workflow (whose CWD is already correct - the repo root) is completely unaffected.
  - builder.Services.AddDataProtection() (Program.cs) needs .PersistKeysToFileSystem(new
    DirectoryInfo(Path.Combine("data", "keys"))) - default key storage persists to the interactive
    user's profile, which a Windows Service account can't reliably rely on; without this, every
    service restart would silently generate new keys and invalidate the auth cookie and anything
    else DataProtection-encrypted. Uses the same "data/"-relative convention as every other path in
    this codebase, so it resolves correctly in both the interactive and (once Increment 2's CWD fix
    applies) service cases.
DELIVERABLES: Program.cs and CarCareTracker.csproj changes; verified no behavior change for the
  existing dev workflow.
ACCEPTANCE CRITERIA:
  - dotnet build: 0 errors.
  - dotnet test Tests/CarCareTracker.Tests.csproj: all passing, no regression.
  - dotnet run --urls http://localhost:5299 still starts normally, /health still passes, and a new
    data/keys/ folder is created (proves the DataProtection change works without requiring the
    service path to verify it).
VALIDATION COMMANDS:
  dotnet build
  dotnet test Tests/CarCareTracker.Tests.csproj
  dotnet run --urls http://localhost:5299 --no-build (background), curl http://localhost:5299/health
STOP CONDITION: All three validation commands green before moving to Increment 2 (which needs the
  user physically present for admin elevation and their phone for Tailscale).
```

### What was done

1. Added the `Microsoft.Extensions.Hosting.WindowsServices` package (`dotnet add package`, resolved
   to 10.0.11) and `builder.Host.UseWindowsService()` right after `WebApplication.CreateBuilder`.
2. While reading `Helper/StaticHelper.cs` to confirm the DataProtection key path convention, found
   the CWD gotcha described above - not anticipated in the original plan, which had only flagged
   DataProtection key persistence. Confirmed all three path constants
   (`DbName`/`UserConfigPath`/`ServerConfigPath`) are literal `"data/..."` strings, and that
   `Program.cs` creates `data/` itself via a bare `Directory.CreateDirectory("data")` - both
   unambiguously CWD-relative, both would break under a service's default `System32` working
   directory. Fixed with a `WindowsServiceHelpers.IsWindowsService()`-gated
   `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` at the very top of `Program.cs`, before
   the config JSON file additions or `StaticHelper.CheckMigration` run - so it only ever changes
   behavior for the not-yet-existing service deployment, never the interactive workflow.
3. Changed `builder.Services.AddDataProtection();` to chain
   `.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine("data", "keys")))`. Needed a
   `using Microsoft.AspNetCore.DataProtection;` addition (the extension method wasn't in scope from
   the ambient `Microsoft.Extensions.Hosting.WindowsServices` using).
4. First build attempt failed on a locked `CarCareTracker.exe` - a leftover `dotnet run` instance
   from an earlier session (PID 21384) still held the file. Killed it (matches this project's
   documented dev workflow of killing any running instance before a rebuild) and rebuilt cleanly.
5. Verified: `dotnet build` 0 errors/0 warnings-from-this-change (224 pre-existing nullable warnings
   unrelated to this work, unchanged); `dotnet test Tests/CarCareTracker.Tests.csproj` 10/10 passing;
   restarted `dotnet run --urls http://localhost:5299 --no-build` in the background, `curl /health`
   returned `{"status":"pass",...}`, and confirmed `data/keys/key-<guid>.xml` was created - proving
   the DataProtection change works correctly under the normal interactive path (the
   `IsWindowsService()` gate correctly stayed false, so `data/` resolved in place as before, not
   under a changed CWD).

### Result

Complete. The app is now safe to register as a Windows Service without a silent data-location or
session-invalidation failure, and the interactive `dotnet run` workflow is unchanged (build, tests,
and a live health-check all confirm no regression). Increment 2 (actually publishing, registering the
service, configuring Kestrel to loopback-only via the existing Setup UI, and setting up Tailscale)
needs the user physically present for admin elevation and their phone - not started yet.

## Increment 2: Publish, install, and register as a persistent Windows Service

### Task packet

```
TASK ID: PHASE-15-02
TITLE: Publish the app, carry over real data, and run it as a persistent Windows Service bound to
  loopback only
OBJECTIVE: Replace the manual "dotnet run in a terminal" dev workflow with a persistent, always-on
  service the phone can eventually reach, without losing or duplicating the user's real vehicle data,
  and without exposing Kestrel beyond loopback (Tailscale, in Increment 3, does the actual external
  exposure).
INPUTS: The Increment 1 code (Program.cs's UseWindowsService()/CWD fix, already verified),
  Views/Home/Setup.cshtml + Helper/ConfigHelper.cs's existing Kestrel-binding mechanism, the real
  data/ directory at the dev repo root (confirmed to contain a real 224KB LiteDB database, not just
  test fixtures - user confirmed this is their actual vehicle data).
ALLOWED SCOPE: Publishing a Release build to a new, dedicated install location outside the dev repo's
  bin/obj; a one-time copy (not move) of the existing data/ folder into that location; directly
  writing data/config/serverConfig.json's Kestrel section (same JSON shape the Setup UI itself
  produces) rather than a UI round-trip; registering and starting the Windows Service (requires the
  user's admin elevation - not something to attempt unattended).
NON-SCOPE: Tailscale installation/configuration (Increment 3); enabling auth (Increment 4); deleting
  or modifying the dev repo's original data/ folder (left untouched as a fallback, per user's explicit
  choice when asked).
IMPLEMENTATION REQUIREMENTS:
  - Publish scoped explicitly to CarCareTracker.csproj (not the .sln, which also pulls in the Tests
    project - discovered empirically on the first attempt, which published CarCareTracker.Tests.dll
    alongside the real app unnecessarily).
  - Install location: C:\Services\CarTracker (user's choice from two offered options, picked over
    a custom path).
  - data/ copy: verified twice - once naively (which nested as data/data/ because the destination
    directory already existed, requiring a redo) and once correctly at the top level, then verified
    the published copy actually reads it (compared GET /api/vehicles between the dev instance and the
    published copy running on a scratch port - identical BMW Z4 record confirmed byte-for-byte
    matching JSON).
  - Kestrel binding: wrote data/config/serverConfig.json's "Kestrel" key directly
    (`{"Endpoints":{"Http":{"Url":"http://127.0.0.1:5299"}}}`), matching KestrelAppConfig's exact
    JsonPropertyName shape, rather than requiring the user to click through the Setup UI. Verified by
    starting the published exe standalone (no --urls override) and confirming via netstat it bound
    only to 127.0.0.1:5299, not also the wildcard/IPv6-any address.
  - Service registration (run by the user in an elevated PowerShell, not by the agent): sc.exe create
    with start=auto, a description, a failure/restart policy (3x restart with 5s delay, resetting the
    failure counter after 24h), then sc.exe start.
DELIVERABLES: A running Windows Service serving the user's real vehicle data on
  http://127.0.0.1:5299, confirmed independently by the agent (query health + vehicle data over the
  loopback address) after the user completed the elevated steps.
ACCEPTANCE CRITERIA:
  - sc.exe query CarTracker reports STATE: 4 RUNNING.
  - GET http://127.0.0.1:5299/health returns {"status":"pass",...}.
  - GET http://127.0.0.1:5299/api/vehicles returns the real vehicle data (BMW Z4, id 1), not an
    empty/fresh database.
  - The service is not reachable on any non-loopback address (netstat shows only 127.0.0.1:5299, no
    0.0.0.0 or wildcard binding).
VALIDATION COMMANDS:
  dotnet publish CarCareTracker.csproj -c Release -o C:\Services\CarTracker
  sc.exe query CarTracker
  curl http://127.0.0.1:5299/health
  curl http://127.0.0.1:5299/api/vehicles
  netstat -ano | findstr 5299
STOP CONDITION: Service confirmed RUNNING with real data, verified independently by the agent (not
  just trusted from the user's screenshot) before moving to Increment 3 (Tailscale).
```

### What was done

1. First publish attempt (`dotnet publish -c Release -o C:\Services\CarTracker`, no project argument)
   resolved to the .sln in the current directory and published both CarCareTracker and
   CarCareTracker.Tests into the same output folder - unwanted test-project clutter in a production
   deployment. Redone scoped explicitly to `CarCareTracker.csproj`.
2. Copied the dev repo's `data/` folder (confirmed to contain a real, non-trivial 224KB
   `cartracker.db` - the user confirmed this is their actual vehicle data, not test fixtures) into the
   publish output. First attempt nested incorrectly as `data/data/...` because the destination
   directory already existed from the aborted first publish; caught by inspecting the copied tree,
   removed, and redone correctly at the top level.
3. Asked the user two questions before touching anything data-related: where to install the published
   app (offered `C:\Services\CarTracker` vs. a custom path - user picked the recommended default), and
   whether to carry the real data over as the service's live copy vs. starting fresh (user confirmed
   yes, carry it over; the dev repo's original `data/` stays untouched as a fallback/backup, not
   deleted).
4. Verified the copy actually works before trusting it: ran the published exe standalone on a scratch
   port (5300) and diffed `GET /api/vehicles` against the still-running dev instance (port 5299) -
   byte-for-byte identical BMW Z4 record confirmed the copy is real and readable, not silently empty.
5. Pre-wrote `data/config/serverConfig.json`'s `"Kestrel"` section directly (matching
   `KestrelAppConfig`'s exact JSON shape - confirmed by reading `Models/Settings/KestrelAppConfig.cs`
   and `ConfigHelper.SaveServerConfig`'s validation logic first) rather than requiring a UI round-trip
   through `/Home/Setup`. Verified empirically: started the published exe with no `--urls` override
   (so only the config file could be driving the binding) and confirmed via `netstat` it bound to
   `127.0.0.1:5299` only - not also the IPv6 loopback or any wildcard address, which is what it had
   done before the config file existed.
6. Hit a real ordering issue during this verification: the dev instance (still running on port 5299
   from Increment 1's testing) blocked the new binding attempt, throwing
   `AddressInUseException`/`SocketException 10048` - not a config bug, just two processes wanting the
   same port. Stopped the dev instance first, then re-verified cleanly.
7. Handed the user exact, copy-pasteable `sc.exe` commands to run in an elevated PowerShell (admin
   rights not available to the agent's shell - confirmed via a `WindowsPrincipal`/`IsInRole` check
   before even attempting), including a description, a failure/restart policy, and explicit
   stop/delete commands for full reversibility if anything went wrong. User ran them and shared a
   screenshot showing `[SC] CreateService SUCCESS` and the service starting.
8. Independently re-verified from the agent's own shell rather than trusting the screenshot alone:
   `sc.exe query CarTracker` → `STATE: 4 RUNNING`; `curl http://127.0.0.1:5299/health` →
   `{"status":"pass",...}`; `curl http://127.0.0.1:5299/api/vehicles` → the real BMW Z4 record,
   confirming the running service is serving the carried-over real data, not a fresh database.

### Result

Complete. `CarTracker` is now a running Windows Service (`start=auto`, restart-on-failure), bound only
to `127.0.0.1:5299`, serving the user's real vehicle data. The dev repo at
`D:\Personal\CarTracker\lubelog` is unaffected and can still be used for `dotnet run` development on a
different port. Increment 3 (Tailscale on the PC and the user's phone, `tailscale serve` for HTTPS,
verified reachable from the phone with home wifi off) is next.

## Increment 3: Tailscale reachability

### Task packet

```
TASK ID: PHASE-15-03
TITLE: Reach the Windows Service from the phone over a private Tailscale network
OBJECTIVE: Prove the phone can load the real app, with real data, over Tailscale specifically (not
  home wifi/LAN), with HTTPS.
INPUTS: The running CarTracker Windows Service bound to 127.0.0.1:5299 (Increment 2); user-installed
  Tailscale on the PC and phone, signed into the same account/tailnet.
ALLOWED SCOPE: Reading Tailscale's own status/DNS name from the CLI; running `tailscale serve` to
  proxy the loopback-only Kestrel endpoint to the tailnet over HTTPS; verifying reachability from the
  agent's own shell (curl) and asking the user to verify from the phone.
NON-SCOPE: Any change to the app itself; Tailscale account/billing configuration beyond the one-time
  "enable Serve" grant (an account-level permission only the user can grant, surfaced by the CLI
  itself when needed).
IMPLEMENTATION REQUIREMENTS:
  - Locate the Tailscale CLI (not on PATH in the agent's shell - found at the standard Windows
    install path, `C:\Program Files\Tailscale\tailscale.exe`) and confirm both devices are actually
    on the same tailnet via `tailscale status` before doing anything else.
  - Get this device's MagicDNS name via `tailscale status --json` (`legion.tail80af14.ts.net`) rather
    than assuming a naming pattern.
  - `tailscale serve --bg http://127.0.0.1:5299` - not the older `tailscale serve https / <target>`
    syntax from the original plan, which the installed Tailscale version rejected (the CLI changed;
    it printed the correct modern replacement itself). Also hit a real Git-Bash gotcha unrelated to
    Tailscale: passing a bare `/` as an argument through Git Bash's MSYS layer gets silently rewritten
    to a Windows path (`C:/Program Files/Git/`) - switched to the PowerShell tool for this command to
    avoid the mangling.
  - First `serve` attempt hung (backgrounded by the harness after a 120s timeout) because "Serve" is
    an account-level feature that needs a one-time enable via a URL Tailscale itself prints
    (`https://login.tailscale.com/f/serve?node=...`) - an account permission grant only the user can
    complete, not something to work around. Stopped the hung task, had the user complete that step,
    then re-ran the same command successfully.
DELIVERABLES: A working `https://legion.tail80af14.ts.net/` endpoint, verified independently by the
  agent (curl) and by the user from their phone.
ACCEPTANCE CRITERIA:
  - `curl https://legion.tail80af14.ts.net/health` (from the PC) returns `{"status":"pass",...}`.
  - The phone, with **wifi off (mobile data only)** and Tailscale toggled on, loads the same URL and
    shows the real app with real data - proving genuine tailnet reachability, not accidental
    same-network access.
VALIDATION COMMANDS:
  tailscale status
  tailscale status --json (for the DNS name)
  tailscale serve --bg http://127.0.0.1:5299
  curl -k https://legion.tail80af14.ts.net/health
  (user, on phone, wifi off) open https://legion.tail80af14.ts.net/
STOP CONDITION: Both the agent's own curl check and the user's phone-side confirmation (wifi off)
  passed before considering this increment done.
```

### What was done

1. `tailscale status` (via full path, since the CLI isn't on this shell's PATH) confirmed both
   devices already on the same tailnet: `legion` (this PC, Windows) and `huzaifas-s25-ultra`
   (the user's phone, Android), both under the same account.
2. Pulled the PC's real MagicDNS name from `tailscale status --json` rather than guessing a pattern:
   `legion.tail80af14.ts.net`.
3. First `tailscale serve` attempt used the plan's originally-assumed syntax
   (`tailscale serve https / http://127.0.0.1:5299`) and failed - the installed version's CLI has
   since changed; it printed the correct modern replacement command itself. Retried with
   `tailscale serve --bg http://127.0.0.1:5299`, run via the PowerShell tool instead of Bash after
   noticing Git Bash's MSYS layer silently rewrites a bare `/` argument into a Windows path (harmless
   here since the new syntax doesn't need one, but worth knowing for future commands).
4. That attempt hung rather than erroring cleanly - the harness backgrounded it after 120s. Read the
   task's output file directly rather than guessing why: "Serve is not enabled on your tailnet",
   with a one-time enable URL tied to the user's account. Stopped the hung background task (`TaskStop`
   - it would never complete on its own once printing that message) and asked the user to open the
   link and approve it - an account-level permission grant, not something available to the agent.
5. Re-ran the identical `serve` command after the user confirmed - succeeded immediately:
   `https://legion.tail80af14.ts.net/` proxying to `http://127.0.0.1:5299`, running in the background
   (persists across the Tailscale daemon's lifetime, no separate "make it permanent" step needed).
6. Verified independently before asking the user to test anything: `curl -k
   https://legion.tail80af14.ts.net/health` from the PC returned `{"status":"pass",...}`.
7. Asked the user to test from the phone with wifi explicitly turned off (mobile data only) - the one
   condition that actually proves tailnet reachability rather than coincidental same-network access.
   Confirmed: the real app loaded with real data.

### Result

Complete. The app is now reachable from the phone from anywhere (verified over mobile data, not just
home wifi), over HTTPS, via Tailscale's private network - never exposed to the public internet, no
port forwarding, no firewall rule added or needed (Kestrel is still loopback-only; Tailscale's own
daemon does the proxying).

## Increment 4: Auth enablement — explicitly declined by the user

The plan's original Increment 4 was to turn on the app's login system, since without it anyone
reaching the tailnet URL gets full unauthenticated root access. Presented to the user with exact
Settings UI steps (`Enable Authentication` switch → root-credential popup → `POST
/Login/CreateLoginCreds`). **User explicitly declined**: both the laptop and phone are already
protected at the device level, and reaching the tailnet URL at all already requires being an
authorized device on this specific private tailnet (Tailscale's own device-authorization layer - see
Increment 3). This is a deliberate, informed decision, not an oversight or something skipped by
default - do not re-raise it unprompted in a future session; if the user raises it themselves later,
the exact UI path is already documented above and nothing else blocks it. `EnableAuth` remains `false`
in the deployed service's `data/config/userConfig.json`.

## Increment 5: PWA install on the phone

The manifest/meta-tag work was already complete before this phase started (confirmed during initial
planning research - `wwwroot/manifest.json`, full icon set, `display: standalone`, viewport/theme-
color/apple-touch meta tags all already in `Views/Shared/_Layout.cshtml`). This increment was just
confirming the install flow actually works once the app is reachable.

### What was done

1. Asked the user which shortcut type they wanted: a full PWA install (uses the existing manifest -
   proper name/icon, opens standalone with no browser chrome) vs. a plain browser bookmark. User chose
   the PWA install, matching the phase's original framing ("the exact same app... on my phone").
2. Walked through the install path (Chrome → ⋮ menu → "Install app", from
   `https://legion.tail80af14.ts.net/` with Tailscale connected).
3. Flagged a real reliability risk specific to the user's phone model (Samsung Galaxy S25 Ultra,
   confirmed from the `tailscale status` device name `huzaifas-s25-ultra` in Increment 3): Samsung's
   battery management is known to aggressively kill backgrounded VPN connections, which would make the
   installed shortcut silently fail to load (Tailscale disconnected) even though nothing is actually
   broken. Recommended two settings, not just one, to avoid a shortcut that "sometimes doesn't work":
   Tailscale's battery usage set to "Unrestricted" (not "Optimized"), and "Always-on VPN" enabled for
   Tailscale in Android's VPN settings - explicitly recommended against also enabling "Block
   connections without VPN" in that same screen, since that would cut off all internet whenever
   Tailscale drops, a worse trade-off than the problem it's meant to prevent for a phone used for
   things other than this app.
4. User confirmed: installed, both settings applied, tapping the home-screen icon opens the app
   full-screen with real data, no browser chrome.

### Result

Complete. The phone has a proper installed "Car Tracker" app icon (not a bookmark), backed by the same
manifest the app already shipped with - no code changes needed. Combined with Tailscale's always-on
VPN setting, this closes the loop the user originally asked for: "the exact same app... on my phone
and synced" - there is one server, one database, and both devices are just clients of it.
