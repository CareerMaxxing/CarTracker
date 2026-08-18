# Desktop-shortcut launcher for Car Tracker.
# Starts the local dev server (if it isn't already running) and opens the app in the default browser.

$ErrorActionPreference = "SilentlyContinue"

$repoRoot = Split-Path -Parent $PSScriptRoot
$port = 5299
$url = "http://localhost:$port"

$listening = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue

if (-not $listening) {
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", $url -WorkingDirectory $repoRoot -WindowStyle Hidden

    $tries = 0
    while (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) -and $tries -lt 60) {
        Start-Sleep -Seconds 1
        $tries++
    }
}

Start-Process $url
