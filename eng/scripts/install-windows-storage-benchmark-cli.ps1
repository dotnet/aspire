param(
    [Parameter(Mandatory)]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$RunId,
    [Parameter(Mandatory)]
    [ValidateSet('baseline', 'ntfs', 'devdrive')]
    [string]$Mode,
    [Parameter(Mandatory)]
    [ValidateRange(1, 5)]
    [int]$Sample
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

if ($env:GITHUB_REPOSITORY -ne 'radical/aspire' -or $env:GITHUB_EVENT_NAME -ne 'workflow_dispatch')
{
    throw 'Benchmark artifact installation is restricted to the fork experiment.'
}

$run = gh api "repos/radical/aspire/actions/runs/$RunId" | ConvertFrom-Json
if ($run.conclusion -ne 'success' -or $run.path -ne '.github/workflows/build-cli-native-archives.yml')
{
    throw 'Consumer validation requires a successful native-build benchmark run.'
}

$root = Join-Path $env:RUNNER_TEMP 'benchmark-artifact'
$archiveRoot = Join-Path $root 'archive'
$storageRoot = Join-Path $root 'storage'
gh run download $RunId --repo radical/aspire --name "cli-native-archives-win-arm64-$Mode-$Sample" --dir $archiveRoot
gh run download $RunId --repo radical/aspire --name "windows-storage-$Mode-$Sample" --dir $storageRoot
$storage = Get-Content -Raw (Join-Path $storageRoot 'storage.json') | ConvertFrom-Json
$build = Get-Content -Raw (Join-Path $storageRoot 'build.json') | ConvertFrom-Json
if ($storage.status -ne 'exercised' -or $storage.mode -ne $Mode -or
    $storage.sample -ne $Sample -or $storage.sourceSha -cne $run.head_sha -or
    $build.sourceSha -cne $run.head_sha)
{
    throw 'The selected artifact lacks matching successful treatment evidence.'
}

$archives = @(Get-ChildItem $archiveRoot -Filter 'aspire-cli-win-arm64-*.zip' -Recurse -File)
if ($archives.Count -ne 1) { throw "Expected exactly one CLI archive; found $($archives.Count)." }
$installRoot = Join-Path $env:TEMP 'benchmark-cli-install'
Expand-Archive -LiteralPath $archives[0].FullName -DestinationPath $installRoot
$cli = Join-Path $installRoot 'aspire.exe'
$version = (& $cli --version | Out-String).Trim()
$archiveVersion = [IO.Path]::GetFileNameWithoutExtension($archives[0].Name).Substring('aspire-cli-win-arm64-'.Length)
# These -ci builds omit commit metadata from --version. Bind source provenance to
# the exact artifact run and its checkout reports; do not emulate a commit identity.
if ($version -cne $archiveVersion)
{
    throw "Installed CLI version does not match archive version $archiveVersion`: $version"
}
$installRoot | Out-File -FilePath $env:GITHUB_PATH -Encoding utf8 -Append

@{
    runId = $RunId
    runAttempt = $run.run_attempt
    sourceSha = $run.head_sha
    mode = $Mode
    sample = $Sample
    archive = $archives[0].Name
    cliPath = $cli
    cliVersion = $version
    sourceIdentityEvidence = 'GitHub artifact run plus matching storage/build checkout reports; CI --version omits source metadata.'
    templateChannel = 'daily'
    templateProvenance = 'Published daily templates and dependencies; the native CLI comes from the selected benchmark artifact.'
} | ConvertTo-Json | Set-Content (Join-Path $env:RUNNER_TEMP 'windows-storage-benchmark/consumer.json')
