<#
.SYNOPSIS
    Publishes the NICK AI desktop app and produces a real NICKAI.exe.

.DESCRIPTION
    Runs the standard .NET publish pipeline. Nothing here fabricates an
    executable: the .exe is whatever dotnet publish actually emits.

.EXAMPLE
    pwsh ./scripts/publish.ps1 -Runtime win-x64

.EXAMPLE
    pwsh ./scripts/publish.ps1 -Runtime win-arm64 -FrameworkDependent
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    # Self-contained bundles the .NET runtime; framework-dependent requires an
    # installed .NET 8 Desktop Runtime on the target machine.
    [switch]$FrameworkDependent
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/NickAI.App/NickAI.App.csproj'
$output = Join-Path $root "publish/$Runtime"
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }

Write-Host "==> Restoring" -ForegroundColor Cyan
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)." }

Write-Host "==> Publishing $Runtime ($Configuration, self-contained=$selfContained)" -ForegroundColor Cyan
if (Test-Path $output) { Remove-Item $output -Recurse -Force }

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained $selfContained `
    -p:PublishSingleFile=false `
    -o $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

$exe = Join-Path $output 'NICKAI.exe'
if (-not (Test-Path $exe)) {
    throw "Publish completed but NICKAI.exe was not produced. Check the AssemblyName in NickAI.App.csproj."
}

Write-Host "==> Published: $exe" -ForegroundColor Green
