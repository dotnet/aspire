param(
    [string] $SourceSha = '',
    [string] $PullRequest = '',
    [string] $SourceRepository = 'microsoft/aspire',
    [ValidateRange(1, 3)]
    [int] $Repetitions = 3,
    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($PullRequest) {
    if ($PullRequest -notmatch '^[1-9][0-9]{0,8}$') {
        throw 'PullRequest must be a positive upstream PR number.'
    }
    if ($SourceRepository -ne 'microsoft/aspire') {
        throw 'SourceRepository applies only to explicit-SHA runs, not upstream PR runs.'
    }
    $headers = @{ Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
    if ($env:GH_TOKEN) {
        $headers.Authorization = "Bearer $env:GH_TOKEN"
    }
    $pr = Invoke-RestMethod -Uri "https://api.github.com/repos/microsoft/aspire/pulls/$PullRequest" -Headers $headers
    if ($SourceSha -and $SourceSha -ne $pr.head.sha) {
        throw 'SourceSha does not match the current upstream PR head.'
    }
    $SourceSha = $pr.head.sha
    $SourceRepository = $pr.head.repo.full_name
}

if ($SourceSha -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'SourceSha must be a full 40-character commit SHA (or supply PullRequest).'
}
if ($SourceRepository -notmatch '^[A-Za-z0-9][A-Za-z0-9-]*/[A-Za-z0-9_][A-Za-z0-9_.-]*$') {
    throw 'SourceRepository must be an owner/repository name on github.com.'
}
if (Test-Path $OutputDirectory) {
    throw 'OutputDirectory must not already exist; use a fresh experiment directory.'
}

$plan = [ordered]@{
    schemaVersion = 1
    experiment = 'shared-managed-test-payload'
    sourceRepository = $SourceRepository
    sourceSha = $SourceSha.ToLowerInvariant()
    pullRequest = $PullRequest
    harnessSha = $env:GITHUB_SHA
    runId = $env:GITHUB_RUN_ID
    runAttempt = $env:GITHUB_RUN_ATTEMPT
    plannedAt = [DateTimeOffset]::UtcNow.ToString('o')
    runner = '8-core-ubuntu-latest'
    repetitions = $Repetitions
    project = 'tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj'
    classes = @(
        'Aspire.Hosting.Tests.StableConnectionStringBuilderTests',
        'Aspire.Hosting.Tests.ResourceBuilderLifetimeTests'
    )
}
$matrix = @{ include = @(1..$Repetitions | ForEach-Object { @{ repetition = $_ } }) }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$plan | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDirectory 'plan.json')
if ($env:GITHUB_OUTPUT) {
    @(
        "source_sha=$($plan.sourceSha)"
        "source_repository=$SourceRepository"
        "repetitions=$($matrix | ConvertTo-Json -Depth 10 -Compress)"
    ) | Add-Content $env:GITHUB_OUTPUT
}
$plan | ConvertTo-Json -Depth 10
