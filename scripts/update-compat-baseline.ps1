#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Moves the compatibility baseline to another published release.

.DESCRIPTION
    test/Linq2GraphQL.Compat checks that an app which updates the runtime packages and keeps its generated client
    behaves as before. It holds a frozen copy of the test clients as the baseline release generated them, and runs
    them against that release's packages and against this code.

    After a release, run this with the new version. It copies the test clients from the release's git tag into
    test/Linq2GraphQL.Compat/FrozenClients, sets Linq2GraphQLBaselineVersion in Directory.Build.props (package
    validation uses it too), clears the accepted differences, which the new baseline contains, and refreshes the
    baseline runner's lock file. Review and commit the result.

.PARAMETER Version
    The published version to compare against; its git tag must exist.

.EXAMPLE
    ./scripts/update-compat-baseline.ps1 -Version 1.0.67-prerelease
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$compatRoot = Join-Path $repoRoot 'test/Linq2GraphQL.Compat'
$frozenClients = Join-Path $compatRoot 'FrozenClients'
$clients = @('Linq2GraphQL.TestClient', 'Linq2GraphQL.TestClientNullable')

git -C $repoRoot rev-parse --verify --quiet "refs/tags/$Version" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "There is no git tag $Version. Run git fetch --tags first." }

$extracted = Join-Path ([System.IO.Path]::GetTempPath()) "linq2graphql-baseline-$([guid]::NewGuid())"
New-Item -ItemType Directory $extracted | Out-Null
try {
    Write-Host "==> Copying the test clients of $Version" -ForegroundColor Cyan
    $archive = Join-Path $extracted 'clients.tar'
    git -C $repoRoot archive --format=tar -o $archive $Version -- @($clients | ForEach-Object { "test/$_" })
    if ($LASTEXITCODE -ne 0) { throw 'git archive failed.' }
    tar -xf $archive -C $extracted
    if ($LASTEXITCODE -ne 0) { throw 'Extracting the test clients failed.' }

    Remove-Item $frozenClients -Recurse -Force -ErrorAction Ignore
    foreach ($client in $clients) {
        $source = Join-Path $extracted "test/$client"
        Get-ChildItem $source -Recurse -File -Filter '*.cs' | ForEach-Object {
            $target = Join-Path $frozenClients $client ([System.IO.Path]::GetRelativePath($source, $_.FullName))
            New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
            Copy-Item $_.FullName $target
        }
    }
}
finally {
    Remove-Item $extracted -Recurse -Force -ErrorAction Ignore
}

Write-Host "==> Setting Linq2GraphQLBaselineVersion to $Version" -ForegroundColor Cyan
$props = Join-Path $repoRoot 'Directory.Build.props'
$content = [System.IO.File]::ReadAllText($props)
$updated = [regex]::Replace($content, '<Linq2GraphQLBaselineVersion>[^<]*</Linq2GraphQLBaselineVersion>',
    "<Linq2GraphQLBaselineVersion>$Version</Linq2GraphQLBaselineVersion>")
if ($updated -eq $content -and $content -notmatch [regex]::Escape(">$Version<")) {
    throw "Linq2GraphQLBaselineVersion was not found in $props."
}
[System.IO.File]::WriteAllText($props, $updated)

Write-Host '==> Clearing the accepted differences' -ForegroundColor Cyan
[System.IO.File]::WriteAllText((Join-Path $compatRoot 'Tests/accepted-differences.json'), "{}`n")

Write-Host '==> Refreshing the baseline runner lock file' -ForegroundColor Cyan
dotnet restore (Join-Path $compatRoot 'Baseline/Linq2GraphQL.Compat.Baseline.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }

Write-Host '==> Done. Review and commit the result.' -ForegroundColor Green
