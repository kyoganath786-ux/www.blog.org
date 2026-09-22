<#
.SYNOPSIS
    Publishes the app and packages NICK_AI_Setup.exe with Inno Setup.

.DESCRIPTION
    Requires Inno Setup 6 (ISCC.exe). If it is not installed the script stops
    with a clear message rather than producing a fake installer.
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$iss = Join-Path $root 'installer/NickAI.iss'
$sourceDir = Join-Path $root "publish/$Runtime"
$outDir = Join-Path $root 'publish/installer'

if (-not (Test-Path $sourceDir)) {
    Write-Host "==> Publish output missing; publishing first." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'publish.ps1') -Runtime $Runtime -Configuration $Configuration
}

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) was not found. Install it from https://jrsoftware.org/isdl.php and re-run this script."
}

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

Write-Host "==> Compiling installer" -ForegroundColor Cyan
& $iscc "/DSourceDir=$sourceDir" "/DOutputDir=$outDir" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed ($LASTEXITCODE)." }

$setup = Join-Path $outDir 'NICK_AI_Setup.exe'
if (-not (Test-Path $setup)) { throw "Installer was not produced." }

Write-Host "==> Installer: $setup" -ForegroundColor Green
