#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates the checked-in test clients from the test servers' live schemas.

.DESCRIPTION
    test/Linq2GraphQL.TestClient and test/Linq2GraphQL.TestClientNullable hold
    generated output that the test suite compiles against. Nothing in the normal
    build refreshes it, so it silently drifts whenever a template or the test
    schema changes.

    This script boots each test server over plain HTTP, runs the generator
    against it with the flags that produced the checked-in output, and writes
    the result back over the Generated folders. CI runs the same script and
    fails if the result differs from what is committed.

.PARAMETER Check
    Do not keep the regenerated output: restore the working tree afterwards and
    exit non-zero if anything differed. Used by CI.

.EXAMPLE
    ./scripts/regenerate-test-clients.ps1
    ./scripts/regenerate-test-clients.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$clients = @(
    @{
        Name      = 'SampleClient'
        Server    = 'test/Linq2GraphQL.TestServer'
        Output    = 'test/Linq2GraphQL.TestClient/Generated'
        Namespace = 'Linq2GraphQL.TestClient'
        Port      = 5180
        Nullable  = $false
    }
    @{
        Name      = 'SampleNullableClient'
        Server    = 'test/Linq2GraphQL.TestServerNullable'
        Output    = 'test/Linq2GraphQL.TestClientNullable/Generated'
        Namespace = 'Linq2GraphQL.TestClientNullable'
        Port      = 5181
        Nullable  = $true
    }
)

function Wait-ForServer([string]$url, [System.Diagnostics.Process]$process) {
    foreach ($attempt in 1..60) {
        if ($process.HasExited) {
            throw "Server exited with code $($process.ExitCode) before becoming ready."
        }

        try {
            Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 2 -SkipHttpErrorCheck | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Server at $url did not become ready in 30 seconds."
}

Push-Location $repoRoot
try {
    Write-Host '==> Building generator and test servers' -ForegroundColor Cyan
    dotnet build Linq2GraphQL.CI.slnf --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    foreach ($client in $clients) {
        $endpoint = "http://127.0.0.1:$($client.Port)/graphql/"
        Write-Host "==> Starting $($client.Server) on $endpoint" -ForegroundColor Cyan

        $server = Start-Process -PassThru -NoNewWindow -FilePath 'dotnet' -ArgumentList @(
            'run', '--project', $client.Server, '--no-build', '--no-launch-profile'
        ) -Environment @{
            ASPNETCORE_URLS        = "http://127.0.0.1:$($client.Port)"
            ASPNETCORE_ENVIRONMENT = "Development"
        }

        try {
            Wait-ForServer -url $endpoint -process $server

            Write-Host "==> Generating $($client.Name) into $($client.Output)" -ForegroundColor Cyan
            $arguments = @(
                'run', '--project', 'src/Linq2GraphQL.Generator', '--no-build', '--no-launch-profile', '--'
                $endpoint
                "-c=$($client.Name)"
                "-n=$($client.Namespace)"
                "-o=$(Join-Path $repoRoot $client.Output)"
                '-s=true'
                '-d=true'
            )
            if ($client.Nullable) { $arguments += '-nu=true' }

            $sentinel = Join-Path $repoRoot "$($client.Output)/Client/$($client.Name).cs"
            $generatedAt = Get-Date

            dotnet @arguments

            # The generator reports schema failures on stdout without a non-zero
            # exit code, so verify it actually wrote fresh output.
            if (-not (Test-Path $sentinel) -or (Get-Item $sentinel).LastWriteTime -lt $generatedAt) {
                throw "Generating $($client.Name) failed - $sentinel was not written."
            }
        }
        finally {
            if (-not $server.HasExited) {
                $server.Kill($true)
            }
            $server.WaitForExit()
        }
    }

    if (-not $Check) {
        Write-Host '==> Done. Review and commit the regenerated output.' -ForegroundColor Green
        exit 0
    }

    $paths = $clients | ForEach-Object { $_.Output }

    # Compare normalized content rather than the bytes on disk. The generator always
    # writes LF, while .gitattributes "text=auto" gives a CRLF working tree on Windows,
    # so git status reports every regenerated file as modified even when its content is
    # unchanged. git diff applies the same normalization git would apply on checkin, so
    # it reports only real drift.
    $diff = @(git diff --stat -- @paths)

    # git diff only compares tracked files, so a newly generated file needs its own check.
    $added = @(git ls-files --others --exclude-standard -- @paths)

    git checkout -- @paths

    if ($diff.Count -gt 0 -or $added.Count -gt 0) {
        Write-Host ''
        Write-Host 'The checked-in test clients do not match the generator output:' -ForegroundColor Red
        if ($diff.Count -gt 0) {
            Write-Host ($diff -join [Environment]::NewLine)
        }
        foreach ($file in $added) {
            Write-Host "new file: $file"
        }
        Write-Host ''
        Write-Host 'Run ./scripts/regenerate-test-clients.ps1 and commit the result.' -ForegroundColor Red
        exit 1
    }

    Write-Host '==> Checked-in test clients match the generator output.' -ForegroundColor Green
}
finally {
    Pop-Location
}
