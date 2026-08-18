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
