param(
    [Parameter(Mandatory)]
    [string] $EvidenceDirectory,
    [Parameter(Mandatory)]
    [ValidateRange(1, 3)]
    [int] $Repetition,
    [Parameter(Mandatory)]
    [string] $JobsPath,
    [Parameter(Mandatory)]
    [string] $RunPath,
    [Parameter(Mandatory)]
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$classes = @('StableConnectionStringBuilderTests', 'ResourceBuilderLifetimeTests')
$runs = @{}
foreach ($arm in @('baseline', 'payload')) {
    foreach ($class in $classes) {
        $directory = Join-Path $EvidenceDirectory "shadow-$Repetition-evidence-$arm-$class"
        $metadata = Get-Content (Join-Path $directory 'measurement.json') -Raw | ConvertFrom-Json
        if ($metadata.status -ne 'success' -or $metadata.arm -ne $arm -or $metadata.class -ne "Aspire.Hosting.Tests.$class") {
            throw "Missing, failed, or mismatched measurement for $arm/$class."
        }
        $files = @(Get-ChildItem $directory -Filter '*.trx' -Recurse)
        if ($files.Count -ne 1) {
            throw "Expected exactly one TRX for $arm/$class; found $($files.Count)."
        }
        # MTP TRX uses a default XML namespace and random execution IDs:
        # <UnitTestResult testId="..." testName="Method(args)" outcome="Passed" />.
        # Compare class/method/display-name identities, not per-run GUIDs.
        [xml] $trx = Get-Content $files[0].FullName -Raw
        $definitions = @{}
        foreach ($test in $trx.SelectNodes("//*[local-name()='TestDefinitions']/*[local-name()='UnitTest']")) {
            $method = $test.SelectSingleNode("*[local-name()='TestMethod']")
            if ($null -eq $method) {
                throw "Missing TestMethod in $arm/$class."
            }
            $definitions[[string]$test.id] = "$($method.className)::$($method.name)"
        }
        $inventory = @(@(
            foreach ($result in $trx.SelectNodes("//*[local-name()='Results']/*[local-name()='UnitTestResult']")) {
                if (-not $definitions.ContainsKey([string]$result.testId)) {
                    throw "Result has no test definition in $arm/$class."
                }
                $identity = $definitions[[string]$result.testId]
                if (-not $identity.StartsWith("Aspire.Hosting.Tests.${class}::", [StringComparison]::Ordinal)) {
                    throw "Unexpected test class in $arm/$class."
                }
                if ($result.outcome -ne 'Passed') {
                    throw "Non-passing test in $arm/${class}: $($result.testName) ($($result.outcome))."
                }
                "${identity}::$($result.testName)::$($result.outcome)"
            }
        ) | Sort-Object)
        if ($inventory.Count -eq 0) {
            throw "Empty selected tests for $arm/$class."
        }
        if (@($inventory | Select-Object -Unique).Count -ne $inventory.Count) {
            throw "Duplicate test identities in $arm/$class."
        }
        $runs["$arm-$class"] = @{ metadata = $metadata; inventory = @($inventory) }
    }
}

$producer = Get-Content (Join-Path $EvidenceDirectory "shadow-$Repetition-evidence-producer/measurement.json") -Raw | ConvertFrom-Json
if ($producer.status -ne 'success' -or $producer.arm -ne 'producer') {
    throw 'Missing or failed payload producer.'
}
$sourceSha = $producer.sourceSha
foreach ($run in $runs.Values) {
    if ($run.metadata.sourceSha -ne $sourceSha -or $run.metadata.sourceRepository -ne $producer.sourceRepository -or
        $run.metadata.repetition -ne $producer.repetition -or $run.metadata.harnessSha -ne $producer.harnessSha) {
        throw 'Provenance mismatch between experiment arms.'
    }
}
foreach ($class in $classes) {
    if (Compare-Object $runs["baseline-$class"].inventory $runs["payload-$class"].inventory) {
        throw "Selected test identities/outcomes differ for $class."
    }
}

$pages = Get-Content $JobsPath -Raw | ConvertFrom-Json
$jobs = @($pages | ForEach-Object { $_.jobs })
$runMetadata = Get-Content $RunPath -Raw | ConvertFrom-Json
$lanes = @{}
foreach ($arm in @('baseline', 'payload')) {
    $names = @($classes | ForEach-Object {
        if ($arm -eq 'baseline') { "Baseline $Repetition / $_" } else { "Payload consumer $Repetition / $_" }
    })
    if ($arm -eq 'payload') {
        $names += "Payload producer $Repetition"
    }
    $laneJobs = @(
        foreach ($name in $names) {
            $matches = @($jobs | Where-Object { $_.name -eq $name -or $_.name.EndsWith(" / $name", [StringComparison]::Ordinal) })
            if ($matches.Count -ne 1 -or $matches[0].status -ne 'completed' -or $matches[0].conclusion -ne 'success') {
                throw "Expected one successful completed job named $name."
            }
            $matches[0]
        }
    )
    $started = @($laneJobs | ForEach-Object { [DateTimeOffset]$_.started_at } | Sort-Object)[0]
    $finished = @($laneJobs | ForEach-Object { [DateTimeOffset]$_.completed_at } | Sort-Object)[-1]
    $lanes[$arm] = @{
        startedAt = $started.ToString('o')
        completedAt = $finished.ToString('o')
        executionSpanSeconds = ($finished - $started).TotalSeconds
        workflowCreatedToCompletionSeconds = ($finished - [DateTimeOffset]$runMetadata.created_at).TotalSeconds
        runnerSeconds = ($laneJobs | ForEach-Object { ([DateTimeOffset]$_.completed_at - [DateTimeOffset]$_.started_at).TotalSeconds } | Measure-Object -Sum).Sum
        jobs = @($laneJobs | ForEach-Object { @{ id = $_.id; name = $_.name; url = $_.html_url } })
    }
}

$summary = [ordered]@{
    sourceSha = $sourceSha
    sourceRepository = $producer.sourceRepository
    harnessSha = $producer.harnessSha
    repetition = $producer.repetition
    matchedTests = @($classes | ForEach-Object { $runs["baseline-$_"].inventory }).Count
    baseline = @($classes | ForEach-Object { $runs["baseline-$_"] })
    producer = $producer
    payload = @($classes | ForEach-Object { $runs["payload-$_"] })
    lanes = $lanes
    runUrl = $runMetadata.html_url
    runAttempt = $runMetadata.run_attempt
    timingScope = 'lane span includes producer-to-consumer scheduling, checkout, transfer, bootstrap, execution and post-job work; workflow-created interval also includes initial queueing, not PR push-to-finish'
}
$summary | ConvertTo-Json -Depth 20 | Set-Content $OutputPath
$summary | ConvertTo-Json -Depth 20
