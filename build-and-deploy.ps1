#requires -Version 5.1
<#
  Build, validate, pack and reinstall the com.jkommer.mpv Stream Deck plugin.
  Run from anywhere; the script uses its own folder as the project root.

  Stream Deck is restarted from the path of the running instance. If it is not running, the path comes from the
  STREAMDECK_EXE environment variable, or else the default install location under Program Files.
#>

$ErrorActionPreference = 'Stop'

# ---- Configuration ----------------------------------------------------------
$ProjectRoot     = $PSScriptRoot
$PluginId        = 'com.jkommer.mpv'
$PluginDir       = Join-Path $ProjectRoot "src\MpvStreamDeckController\bin\Release\$PluginId.sdPlugin"
$PackageFile     = Join-Path $PluginDir   "$PluginId.streamDeckPlugin"
$InstalledDir    = Join-Path $env:APPDATA "Elgato\StreamDeck\Plugins\$PluginId.sdPlugin"
$StreamDeckExe   = if ($env:STREAMDECK_EXE) { $env:STREAMDECK_EXE } else { Join-Path $env:ProgramFiles 'Elgato\StreamDeck\StreamDeck.exe' }
$ProcessName     = 'StreamDeck'      
$TimeoutSec      = 30
$StartupGraceSec = 5               
# -----------------------------------------------------------------------------

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

# Keeps the window open until a key is pressed, then exits with the given code.
function Exit-Script([int]$Code) {
    Write-Host "`nPress any key to close..." -ForegroundColor Yellow
    try {
        $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    } catch {
        # Hosts without raw key support (e.g. PowerShell ISE)
        Read-Host 'Press Enter to close' | Out-Null
    }
    exit $Code
}

function Fail($msg) {
    Write-Host "FAILED: $msg" -ForegroundColor Red
    Exit-Script 1
}

# Runs an external command and aborts the script if the exit code is non-zero.
function Invoke-Checked([string]$Description, [scriptblock]$Command) {
    Write-Step $Description
    & $Command
    if ($LASTEXITCODE -ne 0) { Fail "$Description (exit code $LASTEXITCODE)" }
    Write-Host "OK" -ForegroundColor Green
}

function Wait-Until([scriptblock]$Condition, [int]$Seconds) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $Seconds) {
        if (& $Condition) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return [bool](& $Condition)
}

# Calculates the version from git history. Returns:
#   SemVer   - for the assembly (e.g. 1.2.4-2)
#   Manifest - for the Stream Deck manifest, which wants major.minor.patch.build (e.g. 1.2.4.2)
function Get-GitVersionInfo {
    Write-Step 'GitVersion'
    if (-not (Get-Command dotnet-gitversion -ErrorAction SilentlyContinue)) {
        Fail 'dotnet-gitversion not found. Install it with: dotnet tool install --global GitVersion.Tool --version 6.8.*'
    }

    # GitVersion.Tool 6.x may target an older .NET than the one installed here; allow it to roll forward.
    $previous = $env:DOTNET_ROLL_FORWARD
    $env:DOTNET_ROLL_FORWARD = 'Major'
    try { $output = & dotnet-gitversion | Out-String }
    finally { $env:DOTNET_ROLL_FORWARD = $previous }
    if ($LASTEXITCODE -ne 0) { Fail "dotnet-gitversion (exit code $LASTEXITCODE)`n$output" }

    try { $info = $output | ConvertFrom-Json }
    catch { Fail "Could not read the dotnet-gitversion output:`n$output" }

    if ($null -eq $info.VersionSourceDistance) { Fail 'GitVersion output has no VersionSourceDistance.' }

    Write-Host "OK" -ForegroundColor Green
    [pscustomobject]@{
        SemVer   = $info.SemVer
        Manifest = "$($info.MajorMinorPatch).$($info.VersionSourceDistance)"
    }
}

Push-Location $ProjectRoot
try {
    $version = Get-GitVersionInfo
    Write-Host "Assembly version: $($version.SemVer)    Manifest version: $($version.Manifest)"

    Invoke-Checked 'dotnet clean' { dotnet clean }
    Invoke-Checked 'dotnet build -c Release' { dotnet build -c Release "-p:Version=$($version.SemVer)" }
    Invoke-Checked 'dotnet test' { dotnet test -c Release --no-build --verbosity normal }

    Write-Step "pushd $PluginDir"
    if (-not (Test-Path $PluginDir)) { Fail "Plugin output folder not found: $PluginDir" }
    Push-Location $PluginDir
    try {
        Write-Host "OK" -ForegroundColor Green

        Invoke-Checked 'streamdeck validate' { streamdeck validate }

        # remove stale package first so it isn't bundled/overwritten
        if (Test-Path $PackageFile) { Remove-Item $PackageFile -Force }
        Invoke-Checked 'streamdeck pack' { streamdeck pack . --force --version "$($version.Manifest)" }
        if (-not (Test-Path $PackageFile)) { Fail "Package not found after packing: $PackageFile" }
    }
    finally {
        Pop-Location
    }

    Write-Step 'Stopping Stream Deck'
    $running = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($running) {
        # Restart from wherever it is actually installed. Path can be empty if the process is not accessible.
        if ($running.Path) { $StreamDeckExe = $running.Path }
        taskkill /F /IM "$ProcessName.exe" | Out-Null
        if (-not (Wait-Until { -not (Get-Process -Name $ProcessName -ErrorAction SilentlyContinue) } $TimeoutSec)) {
            Fail "Stream Deck did not exit within $TimeoutSec seconds"
        }
    } else {
        Write-Host 'Stream Deck was not running.'
    }
    Write-Host "OK" -ForegroundColor Green

    Write-Step "Deleting $InstalledDir"
    if (Test-Path $InstalledDir) {
        # Files can stay locked briefly after Stream Deck exits, so keep retrying until the folder is gone.
        $deleted = Wait-Until {
            try { Remove-Item $InstalledDir -Recurse -Force -ErrorAction Stop } catch { }
            -not (Test-Path $InstalledDir)
        } $TimeoutSec
        if (-not $deleted) { Fail "Could not delete $InstalledDir within $TimeoutSec seconds (files may be locked)" }
    } else {
        Write-Host 'Nothing to delete.'
    }
    Write-Host "OK" -ForegroundColor Green

    Write-Step 'Starting Stream Deck'
    if (-not (Test-Path $StreamDeckExe)) { Fail "Not found: $StreamDeckExe" }
    Start-Process -FilePath $StreamDeckExe
    if (-not (Wait-Until { [bool](Get-Process -Name $ProcessName -ErrorAction SilentlyContinue) } $TimeoutSec)) {
        Fail "Stream Deck did not start within $TimeoutSec seconds"
    }
    
    Write-Host "Process is up; waiting $StartupGraceSec s for it to finish initialising..."
    Start-Sleep -Seconds $StartupGraceSec
    Write-Host "OK" -ForegroundColor Green

    Write-Step "Installing $PackageFile"
    Start-Process -FilePath $PackageFile
    Write-Host "OK" -ForegroundColor Green

    Write-Host "`nAll steps completed successfully." -ForegroundColor Green
    Exit-Script 0
}
catch {
    Fail $_.Exception.Message
}
finally {
    Pop-Location
}