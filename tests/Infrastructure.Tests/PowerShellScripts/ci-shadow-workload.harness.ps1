param(
    [string] $WorkloadPath,
    [string] $Root,
    [ValidateSet('roundtrip', 'build-failure', 'test-failure', 'wrong-source')]
    [string] $Scenario
)

$ErrorActionPreference = 'Stop'
$source = Join-Path $Root 'source'
New-Item -ItemType Directory -Path (Join-Path $source 'eng/common') -Force | Out-Null

# Only process boundaries are stubbed. The shipped workload handles its real
# phase execution, archive transfer, manifests, and error/finally paths.
function global:git {
    $global:LASTEXITCODE = 0
    '1111111111111111111111111111111111111111'
}
@'
#!/usr/bin/env bash
set -euo pipefail
echo build >> calls.txt
if [ "${SHADOW_TEST_SCENARIO:-}" = build-failure ]; then exit 17; fi
out=artifacts/bin/Aspire.Hosting.Tests/Debug/net10.0
mkdir -p "$out" .dotnet/shared/Microsoft.NETCore.App/10.0.8
printf test-assembly > "$out/Aspire.Hosting.Tests.dll"
printf '%s' '{"runtimeOptions":{"framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}' > "$out/Aspire.Hosting.Tests.runtimeconfig.json"
'@ | Set-Content (Join-Path $source 'build.sh')
@'
#!/usr/bin/env bash
set -euo pipefail
echo "$1" >> calls.txt
if [ "$1" = --info ]; then echo test-sdk; exit 0; fi
if [ "$1" = msbuild ]; then echo '--timeout 20m'; exit 0; fi
if [ "${SHADOW_TEST_SCENARIO:-}" = test-failure ]; then exit 23; fi
while [ "$1" != --results-directory ]; do shift; done
printf '<TestRun />' > "$2/tests.trx"
'@ | Set-Content (Join-Path $source 'dotnet.sh')
@'
#!/usr/bin/env bash
set -euo pipefail
echo runtime-install >> calls.txt
'@ | Set-Content (Join-Path $source 'eng/common/dotnet-install.sh')
& chmod +x (Join-Path $source 'build.sh') (Join-Path $source 'dotnet.sh')
if ($LASTEXITCODE -ne 0) { throw 'chmod failed.' }
$env:SHADOW_TEST_SCENARIO = $Scenario
$common = @{
    SourceRoot = $source
    SourceSha = $(if ($Scenario -eq 'wrong-source') { '2222222222222222222222222222222222222222' } else { '1111111111111111111111111111111111111111' })
    SourceRepository = 'microsoft/aspire'
    Repetition = 1
    PayloadDirectory = (Join-Path $Root 'payload')
}
if ($Scenario -eq 'roundtrip') {
    & $WorkloadPath @common -Arm producer -OutputDirectory (Join-Path $Root 'producer')
    Remove-Item -LiteralPath (Join-Path $source 'artifacts/bin/Aspire.Hosting.Tests/Debug/net10.0') -Recurse -Force
    & $WorkloadPath @common -Arm payload -Class StableConnectionStringBuilderTests -OutputDirectory (Join-Path $Root 'consumer')
}
else {
    & $WorkloadPath @common -Arm baseline -Class StableConnectionStringBuilderTests -OutputDirectory (Join-Path $Root 'baseline')
}
