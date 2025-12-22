$ErrorActionPreference = 'Stop'
Write-Host "publish-check: starting"

$ownerRepo = $env:INPUT_OWNER_REPO
if ([string]::IsNullOrWhiteSpace($ownerRepo)) { $ownerRepo = $env:GITHUB_REPOSITORY }

$headSha = $env:INPUT_HEAD_SHA
if ([string]::IsNullOrWhiteSpace($headSha)) { $headSha = $env:GITHUB_SHA }

$checkName = $env:INPUT_CHECK_NAME
if ([string]::IsNullOrWhiteSpace($checkName)) { Write-Error 'check_name input is required.' }

$title = $env:INPUT_TITLE
if ([string]::IsNullOrWhiteSpace($title)) { $title = $checkName }

# Summary content precedence: inline > file > empty
$summaryText = ''
$inline = $env:INPUT_SUMMARY_TEXT
if (-not [string]::IsNullOrWhiteSpace($inline)) {
  $summaryText = $inline
} elseif (-not [string]::IsNullOrWhiteSpace($env:INPUT_SUMMARY_FILE)) {
  $summaryPath = $env:INPUT_SUMMARY_FILE
  if (Test-Path $summaryPath) {
    $summaryText = Get-Content -Path $summaryPath -Raw -Encoding UTF8
  } else {
    Write-Host "Summary file not found at $summaryPath. Proceeding with empty summary."
  }
}

$status = $env:INPUT_STATUS
if ([string]::IsNullOrWhiteSpace($status)) { $status = 'completed' }
$conclusion = $env:INPUT_CONCLUSION
$detailsUrl = $env:INPUT_DETAILS_URL

# Optionally discover the check suite id for the current workflow run
$checkSuiteId = $env:INPUT_CHECK_SUITE_ID
$discover = $env:INPUT_DISCOVER_CHECK_SUITE
$suiteSource = 'none'
if ([string]::IsNullOrWhiteSpace($checkSuiteId) -and $discover -eq 'true') {
  $runId = $env:INPUT_WORKFLOW_RUN_ID
  if ([string]::IsNullOrWhiteSpace($runId)) { $runId = $env:GITHUB_RUN_ID }

  # Preferred discovery: filter jobs by name/runner label
  try {
    $jobsEndpoint = "repos/$ownerRepo/actions/runs/$runId/jobs?per_page=100"
    $jobsJson = gh api $jobsEndpoint 2>$null | ConvertFrom-Json

    $desiredName = $env:INPUT_WORKFLOW_JOB_NAME
    $desiredLabel = $env:INPUT_RUNNER_LABEL
    $selectedJob = $null

    if ($jobsJson -and $jobsJson.jobs) {
      $candidates = $jobsJson.jobs
      $totalJobs = $candidates.Count
      if (-not [string]::IsNullOrWhiteSpace($desiredName)) {
        $candidates = $candidates | Where-Object { $_.name -eq $desiredName }
        Write-Host "Filtered jobs by name '$desiredName': $($candidates.Count) of $totalJobs"
      }
      if (-not [string]::IsNullOrWhiteSpace($desiredLabel)) {
        $before = $candidates.Count
        $candidates = $candidates | Where-Object { $_.labels -contains $desiredLabel }
        Write-Host "Filtered jobs by runner label '$desiredLabel': $($candidates.Count) of $before"
      }

      if ($candidates -and $candidates.Count -gt 0) {
        $selectedJob = $candidates[0]
      } elseif ($jobsJson.jobs.Count -gt 0) {
        $selectedJob = $jobsJson.jobs[0]
        Write-Host "No matching job found by filters. Falling back to first job: '$($selectedJob.name)'"
      }
    } else {
      Write-Host "No jobs returned for workflow_run_id=$runId"
    }

    if ($selectedJob -and $selectedJob.check_run_url) {
      $checkRunUrl = $selectedJob.check_run_url
      $checkSuiteId = gh api $checkRunUrl --jq ".check_suite.id" 2>$null
      if (-not [string]::IsNullOrWhiteSpace($checkSuiteId)) { $suiteSource = "job:$checkRunUrl" }
    }
  } catch {
    Write-Host "Primary suite discovery via jobs failed for run $runId. Will try fallback method."
  }

  # Fallback discovery
  if ([string]::IsNullOrWhiteSpace($checkSuiteId)) {
    try {
      $suitesEndpoint = "repos/$ownerRepo/actions/runs/$runId/check-suites"
      $checkSuiteId = gh api $suitesEndpoint --jq ".check_suites[0].id" 2>$null
      if (-not [string]::IsNullOrWhiteSpace($checkSuiteId)) { $suiteSource = "fallback:$suitesEndpoint" }
    } catch {
      Write-Host "Fallback suite discovery failed for run $runId. Continuing without explicit suite id."
    }
  }
}

if (-not [string]::IsNullOrWhiteSpace($env:INPUT_CHECK_SUITE_ID)) {
  $suiteSource = 'explicit-input'
}

$runIdForLog = if ([string]::IsNullOrWhiteSpace($runId)) { $env:GITHUB_RUN_ID } else { $runId }

if (-not [string]::IsNullOrWhiteSpace($checkSuiteId)) {
  Write-Host "Using check_suite_id: $checkSuiteId (source=$suiteSource) for workflow_run_id: $runIdForLog"
} else {
  if ($discover -eq 'true') {
    Write-Error "Failed to determine check_suite_id (source=$suiteSource) for workflow_run_id: $runIdForLog. Refusing to create a check run without an explicit suite to avoid mis-association."
    Write-Host "Diagnostics: Listing jobs and suites for the run to help debugging..."
    try { gh api "repos/$ownerRepo/actions/runs/$runIdForLog/jobs?per_page=100" | Out-Host } catch { Write-Host "Listing jobs failed: $($_.Exception.Message)" }
    try { gh api "repos/$ownerRepo/actions/runs/$runIdForLog/check-suites" | Out-Host } catch { Write-Host "Listing check-suites failed: $($_.Exception.Message)" }
    exit 1
  } else {
    Write-Host "No check_suite_id determined (source=$suiteSource). GitHub will auto-associate the check run. workflow_run_id: $runIdForLog"
  }
}

# Build request body
$body = @{
  name = $checkName
  head_sha = $headSha
  status = $status
  output = @{ title = $title; summary = $summaryText }
}

if (-not [string]::IsNullOrWhiteSpace($conclusion)) { $body["conclusion"] = $conclusion }
if (-not [string]::IsNullOrWhiteSpace($detailsUrl)) { $body["details_url"] = $detailsUrl }
if (-not [string]::IsNullOrWhiteSpace($checkSuiteId)) { $body["check_suite_id"] = $checkSuiteId }

$json = $body | ConvertTo-Json -Depth 10
$postEndpoint = "repos/$ownerRepo/check-runs"
$headers = @(
  'Accept: application/vnd.github+json',
  'X-GitHub-Api-Version: 2022-11-28'
)

Write-Host "Creating check run '$checkName' for $ownerRepo@$headSha (suite=$checkSuiteId)"
$response = $json | gh api --method POST $postEndpoint --input - @($headers | ForEach-Object { '-H'; $_ })
try {
  $created = $response | ConvertFrom-Json -ErrorAction Stop
  $createdId = $created.id
  $createdSuite = $created.check_suite.id
  $createdName = $created.name
  $createdStatus = $created.status
  Write-Host "Created check_run id=$createdId name='$createdName' status=$createdStatus check_suite_id=$createdSuite"
} catch {
  Write-Host "Check run created, but failed to parse response JSON. Raw response:"
  Write-Host $response
}
