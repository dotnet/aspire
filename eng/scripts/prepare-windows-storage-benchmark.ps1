param(
    [ValidateSet('baseline', 'ntfs', 'devdrive')]
    [string]$Mode = 'baseline',
    [ValidateRange(1, 5)]
    [int]$Sample = 1,
    [string]$Suffix = '',
    [string]$RunnerEnvironment = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-BenchmarkDiskDecision
{
    param(
        [object[]]$Disks,
        [bool]$DriveOccupied,
        [bool]$WorkspacePopulated,
        [bool]$WorkspaceIsLink
    )

    if ($DriveOccupied) { return @{ Eligible = $false; Reason = 'D: is already occupied.' } }
    if ($WorkspacePopulated -or $WorkspaceIsLink) { return @{ Eligible = $false; Reason = 'Workspace must be empty and must not be a reparse point.' } }

    # Observed ARM images expose "Microsoft NVMe Direct Disk v2", RAW, ~220 GiB.
    # Disk numbers are not stable. Refuse ambiguous inventories rather than guessing.
    $candidates = @($Disks | Where-Object {
        $_.PartitionStyle -eq 'RAW' -and $_.BusType -eq 'NVMe' -and
        -not $_.IsBoot -and -not $_.IsSystem -and $_.Size -ge 50GB
    })
    if ($candidates.Count -ne 1)
    {
        return @{ Eligible = $false; Reason = "Expected one unused NVMe disk; found $($candidates.Count)." }
    }

    return @{ Eligible = $true; Reason = 'One unused local NVMe disk.'; DiskNumber = $candidates[0].Number }
}

function Assert-BenchmarkTreatment
{
    param(
        [string]$Mode,
        [object]$WorkspaceVolume,
        [int]$ExpectedDisk = -1,
        [int]$ActualDisk = -1,
        [string]$DevDriveQuery = '',
        [int]$DevDriveQueryExitCode = -1
    )

    $expectedDrive = if ($Mode -eq 'baseline') { 'C' } else { 'D' }
    $expectedFileSystem = if ($Mode -eq 'devdrive') { 'ReFS' } else { 'NTFS' }
    if ($WorkspaceVolume.DriveLetter -ne $expectedDrive -or $WorkspaceVolume.FileSystem -ne $expectedFileSystem)
    {
        throw "Workspace is not on the requested $expectedDrive`: $expectedFileSystem volume."
    }
    if ($Mode -ne 'baseline' -and $ActualDisk -ne $ExpectedDisk)
    {
        throw 'Workspace volume is not on the selected NVMe disk.'
    }
    # On the English hosted image, fsutil reports e.g. "This is a trusted developer volume."
    # A non-Dev-Drive query can also exit 0, with "This is not a developer volume."
    if ($Mode -eq 'devdrive' -and ($DevDriveQueryExitCode -ne 0 -or
        $DevDriveQuery -notmatch '(?m)^This is (?:a trusted|an untrusted) developer volume\.?\s*$'))
    {
        throw 'The requested ReFS volume was not positively identified as a Dev Drive.'
    }
}

# Dot-sourcing exposes the real safety decision to the behavioral tests without disk access.
if ($MyInvocation.InvocationName -eq '.') { return }

if ($env:GITHUB_ACTIONS -ne 'true' -or $env:GITHUB_REPOSITORY -ne 'radical/aspire' -or
    $env:GITHUB_EVENT_NAME -ne 'workflow_dispatch' -or $env:RUNNER_ARCH -ne 'ARM64' -or
    $env:OS -ne 'Windows_NT' -or $RunnerEnvironment -ne 'github-hosted')
{
    throw 'Storage provisioning is restricted to manually dispatched GitHub-hosted Windows ARM64 jobs in radical/aspire.'
}
if ($Suffix -cne "-$Mode-$Sample")
{
    throw 'Benchmark artifact suffix must identify the selected mode and sample.'
}

$reportDirectory = Join-Path $env:RUNNER_TEMP 'windows-storage-benchmark'
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
$reportPath = Join-Path $reportDirectory 'storage.json'
$timer = [Diagnostics.Stopwatch]::StartNew()
$report = [ordered]@{
    schemaVersion = 1
    mode = $Mode
    sample = $Sample
    status = 'not-exercised'
    sourceSha = $env:GITHUB_SHA
    runId = $env:GITHUB_RUN_ID
    runAttempt = $env:GITHUB_RUN_ATTEMPT
    runnerImage = $env:ImageOS
    imageVersion = $env:ImageVersion
    architecture = $env:RUNNER_ARCH
    runnerEnvironment = $RunnerEnvironment
    powershell = $PSVersionTable.PSVersion.ToString()
    pathsBefore = @{
        workspace = $env:GITHUB_WORKSPACE
        runnerTemp = $env:RUNNER_TEMP
        temp = $env:TEMP
        tmp = $env:TMP
    }
}

try
{
    $report.stage = 'disk inventory'
    $disks = @(Get-Disk)
    $report.disks = @($disks | Select-Object Number, FriendlyName, BusType, PartitionStyle, Size, IsBoot, IsSystem)
    $report.stage = 'machine inventory'
    $report.machine = Get-CimInstance Win32_ComputerSystem | Select-Object NumberOfLogicalProcessors, TotalPhysicalMemory
    $report.processor = @(Get-CimInstance Win32_Processor | Select-Object Name, Manufacturer, MaxClockSpeed)
    $report.os = Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber
    $report.stage = 'volume inventory'
    $report.volumesBefore = @(Get-Volume | Select-Object DriveLetter, FileSystem, Size, SizeRemaining)
    $report.stage = 'Defender inventory'
    $report.defender = Get-MpComputerStatus | Select-Object AntivirusEnabled, RealTimeProtectionEnabled, IsTamperProtected, AMProductVersion
    $preferences = Get-MpPreference
    $report.defenderPerformanceModeAvailable = $null -ne $preferences.PSObject.Properties['PerformanceModeStatus']
    $report.defenderPreferences = $preferences | Select-Object ExclusionPath, DisableRealtimeMonitoring, PerformanceModeStatus
    $report.stage = 'provisioning'
    if ($Mode -ne 'baseline')
    {
        $workspace = Get-Item -LiteralPath $env:GITHUB_WORKSPACE
        $decision = Get-BenchmarkDiskDecision -Disks $disks `
            -DriveOccupied (Test-Path -LiteralPath 'D:\') `
            -WorkspacePopulated (@(Get-ChildItem -LiteralPath $workspace.FullName -Force).Count -ne 0) `
            -WorkspaceIsLink (($workspace.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)
        $report.decision = $decision
        if (-not $decision.Eligible)
        {
            throw "Benchmark not exercised: $($decision.Reason)"
        }

        # Recheck immediately before initialization; never reuse or reformat an existing volume.
        $disk = Get-Disk -Number $decision.DiskNumber
        $recheck = Get-BenchmarkDiskDecision -Disks @($disk) `
            -DriveOccupied (Test-Path -LiteralPath 'D:\') -WorkspacePopulated $false -WorkspaceIsLink $false
        if (-not $recheck.Eligible) { throw "Disk eligibility changed: $($recheck.Reason)" }
        if ($disk.IsOffline) { $disk | Set-Disk -IsOffline $false }
        if ($disk.IsReadOnly) { $disk | Set-Disk -IsReadOnly $false }
        $partition = $disk | Initialize-Disk -PartitionStyle GPT -PassThru |
            New-Partition -DriveLetter D -UseMaximumSize
        if ($Mode -eq 'devdrive')
        {
            $partition | Format-Volume -FileSystem ReFS -DevDrive -NewFileSystemLabel AspireBenchmark -Confirm:$false -Force | Out-Null
        }
        else
        {
            $partition | Format-Volume -FileSystem NTFS -NewFileSystemLabel AspireBenchmark -Confirm:$false -Force | Out-Null
        }

        $target = 'D:\aspire-benchmark\workspace'
        $temp = 'D:\aspire-benchmark\tmp'
        New-Item -ItemType Directory -Path $target, $temp | Out-Null
        Set-Location -LiteralPath $env:RUNNER_TEMP
        [Environment]::CurrentDirectory = $env:RUNNER_TEMP
        # No recursive removal: if the workspace acquired any files, removal must fail.
        Remove-Item -LiteralPath $env:GITHUB_WORKSPACE -Force
        New-Item -ItemType Junction -Path $env:GITHUB_WORKSPACE -Target $target | Out-Null
        $marker = Join-Path $env:GITHUB_WORKSPACE 'storage-placement-probe'
        [IO.File]::WriteAllText($marker, $env:GITHUB_SHA)
        if ([IO.File]::ReadAllText((Join-Path $target 'storage-placement-probe')) -cne $env:GITHUB_SHA)
        {
            throw 'Workspace write did not reach the selected disk.'
        }
        $report.workspaceVolume = Get-Volume -FilePath $marker | Select-Object DriveLetter, FileSystem, UniqueId
        $report.workspaceDisk = (Get-Partition -DriveLetter D).DiskNumber
        Remove-Item -LiteralPath $marker
        $env:TEMP = $temp
        $env:TMP = $temp
        "TEMP=$temp", "TMP=$temp" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    }
    else
    {
        $marker = Join-Path $env:GITHUB_WORKSPACE 'storage-placement-probe'
        [IO.File]::WriteAllText($marker, $env:GITHUB_SHA)
        $report.workspaceVolume = Get-Volume -FilePath $marker | Select-Object DriveLetter, FileSystem, UniqueId
        Remove-Item -LiteralPath $marker
    }

    $report.stage = 'treatment verification'
    $report.volumesAfter = @(Get-Volume | Select-Object DriveLetter, FileSystem, Size, SizeRemaining)
    $report.pathsAfter = @{
        workspace = $env:GITHUB_WORKSPACE
        workspaceTarget = (Get-Item -LiteralPath $env:GITHUB_WORKSPACE).Target
        runnerTemp = $env:RUNNER_TEMP
        temp = $env:TEMP
        tmp = $env:TMP
    }
    $report.devDriveQuery = (& fsutil devdrv query 2>&1 | Out-String)
    $report.devDriveQueryExitCode = $LASTEXITCODE
    if ($Mode -ne 'baseline')
    {
        $report.volumeDevDriveQuery = (& fsutil devdrv query D: 2>&1 | Out-String)
        $report.volumeDevDriveQueryExitCode = $LASTEXITCODE
    }
    $assertion = @{ Mode = $Mode; WorkspaceVolume = $report.workspaceVolume }
    if ($Mode -ne 'baseline')
    {
        $assertion.ExpectedDisk = $decision.DiskNumber
        $assertion.ActualDisk = $report.workspaceDisk
        $assertion.DevDriveQuery = $report.volumeDevDriveQuery
        $assertion.DevDriveQueryExitCode = $report.volumeDevDriveQueryExitCode
    }
    Assert-BenchmarkTreatment @assertion
    $report.status = 'exercised'
    $report.stage = 'complete'
}
catch
{
    $report.error = $_.Exception.Message
    throw
}
finally
{
    $report.provisioningSeconds = $timer.Elapsed.TotalSeconds
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
}

# fsutil diagnostics can be unsupported; preserve their exit codes in evidence, not the step exit.
exit 0
