<#
.SYNOPSIS
    Restores and builds the NICK AI solution.
.EXAMPLE
    pwsh ./scripts/build.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    # Skip the test pass (the one-click build runs it by default).
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'NickAI.sln'

Write-Host "==> Restoring $solution" -ForegroundColor Cyan
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)." }

Write-Host "==> Building ($Configuration)" -ForegroundColor Cyan
dotnet build $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }

if (-not $SkipTests) {
    $testProject = Join-Path $root 'tests\NickAI.Core.Tests\NickAI.Core.Tests.csproj'
    Write-Host "==> Testing" -ForegroundColor Cyan
    dotnet test $testProject -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed ($LASTEXITCODE)." }
}

Write-Host "==> Build succeeded." -ForegroundColor Green
