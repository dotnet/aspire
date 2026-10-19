param(
    [Parameter(Mandatory)]
    [ValidateSet('baseline', 'producer', 'payload')]
    [string] $Arm,
    [ValidateSet('StableConnectionStringBuilderTests', 'ResourceBuilderLifetimeTests')]
    [string] $Class,
    [Parameter(Mandatory)]
    [string] $SourceRoot,
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string] $SourceSha,
    [Parameter(Mandatory)]
    [string] $SourceRepository,
    [Parameter(Mandatory)]
    [ValidateRange(1, 3)]
    [int] $Repetition,
    [Parameter(Mandatory)]
    [string] $OutputDirectory,
    [Parameter(Mandatory)]
    [string] $PayloadDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:MSBUILDTERMINALLOGGER = 'false'

if ($IsWindows) {
    throw 'This workload requires a Unix host; the hosted experiment uses Linux.'
}
if ($Arm -ne 'producer' -and -not $Class) {
    throw 'A selected class is required for test consumers.'
}
if (Test-Path $OutputDirectory) {
    throw 'OutputDirectory must be fresh.'
}
$SourceRoot = (Resolve-Path $SourceRoot).Path
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$measurement = [ordered]@{
    schemaVersion = 1
    arm = $Arm
    class = $(if ($Class) { "Aspire.Hosting.Tests.$Class" } else { '' })
    sourceSha = $SourceSha
    sourceRepository = $SourceRepository
    harnessSha = $env:GITHUB_SHA
    repetition = $Repetition
    runId = $env:GITHUB_RUN_ID
    runAttempt = $env:GITHUB_RUN_ATTEMPT
    runner = $(if ($env:GITHUB_ACTIONS -eq 'true') { '8-core-ubuntu-latest' } else { 'local-unix' })
    imageOS = $env:ImageOS
    imageVersion = $env:ImageVersion
    cpuCount = [Environment]::ProcessorCount
    os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    startedAt = [DateTimeOffset]::UtcNow.ToString('o')
    status = 'running'
    phases = [Collections.Generic.List[object]]::new()
}

function Invoke-MeasuredCommand([string] $Name, [string] $Command, [string[]] $Arguments) {
    $started = [DateTimeOffset]::UtcNow
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $exitCode = -1
    try {
        & $Command @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $OutputDirectory "$Name.log")
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "$Name failed with exit code $exitCode."
        }
    }
    finally {
        $measurement.phases.Add(@{
            name = $Name
            command = $Command
            arguments = $Arguments
            startedAt = $started.ToString('o')
            finishedAt = [DateTimeOffset]::UtcNow.ToString('o')
            elapsedSeconds = $watch.Elapsed.TotalSeconds
            exitCode = $exitCode
        })
    }
}

Push-Location $SourceRoot
try {
    $actualSha = & git --no-pager rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $actualSha -ne $SourceSha) {
        throw "Checkout is not the pinned source SHA $SourceSha."
    }
    $env:PLAYWRIGHT_INSTALLED = 'false'
    $env:TEST_LOG_PATH = Join-Path $OutputDirectory 'test-logs'
    $testDirectory = Join-Path $SourceRoot 'artifacts/bin/Aspire.Hosting.Tests/Debug/net10.0'
    $project = Join-Path $SourceRoot 'tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj'
    if ($Arm -eq 'payload') {
        # The repository wrapper bootstraps the SDK/runtimes from this source's
        # global.json without a project restore or MSBuild invocation.
        Invoke-MeasuredCommand 'bootstrap' './dotnet.sh' @('--info')
        $options = Get-Content (Join-Path $PayloadDirectory 'options.json') -Raw | ConvertFrom-Json
        if ($options.sourceSha -ne $SourceSha -or $options.sourceRepository -ne $SourceRepository -or
            $options.repetition -ne $Repetition -or $options.harnessSha -ne $env:GITHUB_SHA) {
            throw 'Payload provenance does not match this consumer.'
        }
        foreach ($runtime in $options.runtimes) {
            if ($runtime.name -notin @('dotnet', 'aspnetcore') -or $runtime.version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$') {
                throw 'Invalid payload runtime manifest.'
            }
            # This is Arcade's wrapper, not the upstream installer: it accepts
            # single-dash arguments and always installs into the source .dotnet.
            Invoke-MeasuredCommand "install-$($runtime.name)" 'bash' @(
                'eng/common/dotnet-install.sh', '-runtime', $runtime.name, '-version', $runtime.version
            )
        }
        $archive = Join-Path (Resolve-Path $PayloadDirectory).Path 'tests.tar.gz'
        $entries = & tar -tzf $archive
        if ($LASTEXITCODE -ne 0 -or -not $entries) {
            throw 'Payload archive is missing or empty.'
        }
        foreach ($entry in $entries) {
            if ($entry -match '(^/|(^|/)\.\.(/|$)|\\)') {
                throw "Unsafe archive entry: $entry"
            }
        }
        if (Test-Path $testDirectory) {
            throw 'Payload consumer must not have an existing test output directory.'
        }
        New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
        Invoke-MeasuredCommand 'extract' 'tar' @('-xzf', $archive, '-C', $testDirectory)
    }
    else {
        Invoke-MeasuredCommand 'build' './build.sh' @(
            '-restore', '-ci', '-build',
            '-projects', $project,
            '/p:InstallBrowsersForPlaywright=false'
        )
        Invoke-MeasuredCommand 'test-options' './dotnet.sh' @('msbuild', $project, '-nologo', '-getProperty:MtpBaseArgs')
        $mtpBaseArgs = (Get-Content (Join-Path $OutputDirectory 'test-options.log') -Raw).Trim()
        if (-not $mtpBaseArgs -or $mtpBaseArgs -match '["'']') {
            throw 'Expected nonempty whitespace-delimited MtpBaseArgs from the source project.'
        }
        $runtimeConfig = Get-Content (Join-Path $testDirectory 'Aspire.Hosting.Tests.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
        $frameworks = @()
        if ($runtimeConfig.runtimeOptions.ContainsKey('frameworks')) {
            $frameworks = @($runtimeConfig.runtimeOptions.frameworks)
        }
        elseif ($runtimeConfig.runtimeOptions.ContainsKey('framework')) {
            $frameworks = @($runtimeConfig.runtimeOptions.framework)
        }
        $runtimes = @(
            foreach ($framework in $frameworks) {
                $requested = [Version]$framework.version
                $installed = @(Get-ChildItem (Join-Path $SourceRoot ".dotnet/shared/$($framework.name)") -Directory |
                    Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' -and ([Version]$_.Name).Major -eq $requested.Major -and ([Version]$_.Name).Minor -eq $requested.Minor } |
                    Sort-Object { [Version]$_.Name } -Descending)
                if ($installed.Count -eq 0) {
                    throw "No compatible local runtime found for $($framework.name) $($framework.version)."
                }
                $runtimeName = switch ($framework.name) {
                    'Microsoft.NETCore.App' { 'dotnet' }
                    'Microsoft.AspNetCore.App' { 'aspnetcore' }
                    default { throw "Unsupported test runtime $($framework.name)." }
                }
                @{ name = $runtimeName; version = $installed[0].Name }
            }
        )
        if ($runtimes.Count -eq 0) {
            throw 'Test runtime configuration did not specify any frameworks.'
        }
        $options = @{
            sourceSha = $SourceSha
            sourceRepository = $SourceRepository
            repetition = $Repetition
            harnessSha = $env:GITHUB_SHA
            mtpBaseArgs = @($mtpBaseArgs -split '\s+')
            runtimes = $runtimes
        }
    }
    $assembly = Join-Path $testDirectory 'Aspire.Hosting.Tests.dll'
    if (-not (Test-Path $assembly -PathType Leaf)) {
        throw "Expected built test assembly at $assembly."
    }
    $measurement.runtimes = $options.runtimes
    if ($Arm -eq 'producer') {
        if (Test-Path $PayloadDirectory) {
            throw 'PayloadDirectory must be fresh for the producer.'
        }
        New-Item -ItemType Directory -Path $PayloadDirectory | Out-Null
        $PayloadDirectory = (Resolve-Path $PayloadDirectory).Path
        $options | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $PayloadDirectory 'options.json')
        $files = @(Get-ChildItem $testDirectory -Recurse -File)
        if (@(Get-ChildItem $testDirectory -Recurse | Where-Object LinkType).Count -ne 0) {
            throw 'Payload must contain regular files/directories, not symbolic links.'
        }
        $files | ForEach-Object {
            @{ path = [IO.Path]::GetRelativePath($testDirectory, $_.FullName); bytes = $_.Length }
        } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutputDirectory 'payload-inventory.json')
        Invoke-MeasuredCommand 'archive' 'tar' @('-czf', (Join-Path $PayloadDirectory 'tests.tar.gz'), '-C', $testDirectory, '.')
        $measurement.payloadBytes = (Get-Item (Join-Path $PayloadDirectory 'tests.tar.gz')).Length
    }
    else {
        # Identical direct MTP invocation in both arms isolates payload reuse
        # from changes in MSBuild's dotnet-test orchestration or Helix settings.
        $testArguments = @($assembly) + @($options.mtpBaseArgs) + @(
            '--filter-class', "Aspire.Hosting.Tests.$Class",
            '--filter-not-trait', 'quarantined=true',
            '--filter-not-trait', 'outerloop=true',
            '--filter-not-trait', 'category=failing',
            '--report-trx', '--report-trx-filename', 'tests.trx',
            '--results-directory', $OutputDirectory
        )
        Invoke-MeasuredCommand 'tests' './dotnet.sh' $testArguments
        if (@(Get-ChildItem $OutputDirectory -Filter '*.trx').Count -ne 1) {
            throw 'Test runner did not produce exactly one TRX.'
        }
    }
    $measurement.status = 'success'
}
catch {
    $measurement.status = 'failure'
    $measurement.error = $_.Exception.Message
    throw
}
finally {
    $measurement.finishedAt = [DateTimeOffset]::UtcNow.ToString('o')
    $measurement | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDirectory 'measurement.json')
    Pop-Location
}
