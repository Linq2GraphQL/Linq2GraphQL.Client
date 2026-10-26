#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Spike: builds "standalone" SampleClients that carry their own copy of the runtime.

.DESCRIPTION
    A standalone client is the generated SampleClient plus the runtime source
    (Linq2GraphQL.Client + Linq2GraphQL.Client.Subscriptions) with the Linq2GraphQL.Client
    namespace rewritten to <client namespace>.Runtime. Its project has no reference to the
    runtime packages.

    Writes three of them:
      test/Linq2GraphQL.TestClientStandalone  current source, namespace Linq2GraphQL.TestClient
      test/Linq2GraphQL.TestClientSecond      current source, namespace Linq2GraphQL.SecondClient
      test/Linq2GraphQL.TestClientLegacy      source from -LegacySource, namespace Linq2GraphQL.LegacyClient

    The existing end-to-end tests that only depend on SampleClient and the runtime are
    copied into test/Linq2GraphQL.Tests.Standalone with the same rewrite, so the same
    assertions run against the standalone copy. The other two clients exist so a test can
    load several standalone clients into one process.

.PARAMETER LegacySource
    Root of an older checkout, e.g. extracted with
    git archive 1.0.30-prerelease src test/Linq2GraphQL.TestClient | tar -x -C <dir>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$LegacySource
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# Tests that need the generator, the AI package or the nullable client stay in the main suite.
$excludedTests = @(
    'AgentLoopTests.cs', 'AgentToolsExtensionsTests.cs', 'AgentToolsetTests.cs',
    'DescriptionAttributeTests.cs', 'ExampleFormatterTests.cs', 'GeneratorCollection.cs',
    'QueryNullableTests.cs', 'QueryValidatorTests.cs', 'SampleClientNullableFixture.cs',
    'ScalarMappingTests.cs', 'ScalarTests.cs', 'SchemaRendererTests.cs', 'ScopedSchemaTests.cs',
    'SdlInputTests.cs', 'SdlOutputTests.cs'
)

function Copy-Rewritten([string]$source, [string]$destination, [string]$clientNamespace, [string[]]$exclude = @()) {
    if (Test-Path $destination) { Remove-Item -Recurse -Force $destination }

    Get-ChildItem -Path $source -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $exclude -notcontains $_.Name } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($source, $_.FullName)
            $target = Join-Path $destination $relative
            New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null

            # Runtime first: its replacement contains the client namespace, which the
            # second pattern must not see as the original Linq2GraphQL.TestClient.
            $text = Get-Content -Raw $_.FullName
            $text = $text -replace '\bLinq2GraphQL\.Client\b', "$clientNamespace.Runtime"
            $text = $text -replace '\bLinq2GraphQL\.TestClient\b', $clientNamespace
            Set-Content -NoNewline -Path $target -Value $text
        }
}

function New-StandaloneClient([string]$sourceRoot, [string]$project, [string]$clientNamespace) {
    Write-Host "==> $project ($clientNamespace) from $sourceRoot" -ForegroundColor Cyan
    $projectDir = Join-Path $repoRoot "test/$project"

    # Assembly.cs only holds InternalsVisibleTo for the package assemblies; the copy lives
    # in the client's own assembly, so it is not needed.
    Copy-Rewritten (Join-Path $sourceRoot 'src/Linq2GraphQL.Client') (Join-Path $projectDir 'Runtime/Client') $clientNamespace @('Assembly.cs')
    Copy-Rewritten (Join-Path $sourceRoot 'src/Linq2GraphQL.Client.Subscriptions') (Join-Path $projectDir 'Runtime/Subscriptions') $clientNamespace @('Assembly.cs')
    Copy-Rewritten (Join-Path $sourceRoot 'test/Linq2GraphQL.TestClient/Generated') (Join-Path $projectDir 'Generated') $clientNamespace
}

New-StandaloneClient $repoRoot 'Linq2GraphQL.TestClientStandalone' 'Linq2GraphQL.TestClient'
New-StandaloneClient $repoRoot 'Linq2GraphQL.TestClientSecond' 'Linq2GraphQL.SecondClient'
New-StandaloneClient $LegacySource 'Linq2GraphQL.TestClientLegacy' 'Linq2GraphQL.LegacyClient'

Write-Host '==> Copying tests' -ForegroundColor Cyan
Copy-Rewritten (Join-Path $repoRoot 'test/Linq2GraphQL.Tests') (Join-Path $repoRoot 'test/Linq2GraphQL.Tests.Standalone/Tests') 'Linq2GraphQL.TestClient' $excludedTests

Write-Host '==> Done' -ForegroundColor Green
