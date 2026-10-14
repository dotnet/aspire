param(
    [Parameter(Mandatory)]
    [ValidateSet('Capture', 'Verify', 'Smoke')]
    [string]$Stage,
    [Parameter(Mandatory)]
    [string]$Rid,
    [string]$SnapshotName = 'first-publish.json'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repo
$evidence = Join-Path $repo 'artifacts/log/shared-bundle'
$null = New-Item -ItemType Directory -Force -Path $evidence
$sdkVersion = (Get-Content global.json -Raw | ConvertFrom-Json).sdk.version
Add-Type -Path (Join-Path $repo ".dotnet/sdk/$sdkVersion/Sdks/Microsoft.Build.Tasks.Git/tools/netframework/System.IO.Hashing.dll")
$exeSuffix = if ($Rid.StartsWith('win-')) { '.exe' } else { '' }

function Get-Manifest([string]$Directory) {
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        throw "Manifest directory missing: $Directory"
    }
    $root = (Resolve-Path -LiteralPath $Directory).Path
    @(
        Get-ChildItem -LiteralPath $root -Recurse -File -Force | ForEach-Object {
            $hash = [System.IO.Hashing.XxHash3]::new()
            $stream = [IO.File]::OpenRead($_.FullName)
            try {
                $hash.Append($stream)
                [pscustomobject]@{
                    path = [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
                    length = $_.Length
                    xxh3 = [Convert]::ToHexString($hash.GetCurrentHash())
                }
            }
            finally { $stream.Dispose() }
        } | Sort-Object path
    )
}

function Save-Manifest([object[]]$Manifest, [string]$Name) {
    ConvertTo-Json -InputObject @($Manifest) -Depth 5 | Set-Content (Join-Path $evidence $Name)
}

function Assert-EqualManifest([object[]]$Expected, [object[]]$Actual, [string]$Label) {
    $changes = @(Compare-Object -ReferenceObject @($Expected) -DifferenceObject @($Actual) -Property path, length, xxh3)
    if ($changes.Count) { throw "$Label content differs: $($changes | ConvertTo-Json -Depth 5)" }
    Write-Host "$Label complete content identical: $($Actual.Count) files."
}

function Get-PayloadManifest([object[]]$Manifest) {
    @($Manifest | Where-Object { $_.path -notmatch '(^|/)[^/]+\.dSYM/|\.(pdb|dbg)$' })
}

function Assert-WindowsMachine([string]$Directory) {
    if (-not $Rid.StartsWith('win-')) { return }
    $expected = if ($Rid -eq 'win-arm64') { 0xAA64 } else { 0x8664 }
    foreach ($file in @('Aspire.Dashboard.exe', 'e_sqlite3.dll')) {
        $reader = [IO.BinaryReader]::new([IO.File]::OpenRead((Join-Path $Directory $file)))
        try {
            $reader.BaseStream.Position = 0x3C
            $offset = $reader.ReadUInt32()
            $reader.BaseStream.Position = $offset
            if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne $expected) {
                throw "$file does not have the expected PE machine for $Rid."
            }
        }
        finally { $reader.Dispose() }
    }
}

switch ($Stage) {
    'Capture' {
        $publish = Join-Path $repo "artifacts/bin/Aspire.Dashboard/$Rid/Release/net11.0/$Rid/publish"
        Assert-WindowsMachine $publish
        Save-Manifest (Get-Manifest $publish) $SnapshotName
    }
    'Verify' {
        $first = @(Get-Content (Join-Path $evidence 'first-publish.json') -Raw | ConvertFrom-Json)
        $after = @(Get-Content (Join-Path $evidence 'after-publish.json') -Raw | ConvertFrom-Json)
        Assert-EqualManifest $first $after 'Prepared Dashboard before/after packaging'
        $package = @(Get-ChildItem artifacts/input/nugets -Recurse -File -Filter "Aspire.Dashboard.Sdk.$Rid.*.nupkg")
        $standalone = @(Get-ChildItem artifacts/input/dashboard -Recurse -File -Filter "aspire-dashboard-$Rid.zip")
        $archiveExtension = if ($exeSuffix) { 'zip' } else { 'tar.gz' }
        $cliArchive = @(Get-ChildItem artifacts/input/cli -Recurse -File -Filter "aspire-cli-$Rid-*.$archiveExtension")
        if ($package.Count -ne 1 -or $standalone.Count -ne 1 -or $cliArchive.Count -ne 1) {
            throw 'Expected exactly one original Dashboard package, standalone archive, and native CLI archive.'
        }
        [IO.Compression.ZipFile]::ExtractToDirectory($package[0].FullName, (Join-Path $evidence 'dashboard-package'))
        [IO.Compression.ZipFile]::ExtractToDirectory($standalone[0].FullName, (Join-Path $evidence 'standalone-dashboard'))
        $packageManifest = @(Get-Manifest (Join-Path $evidence 'dashboard-package/tools'))
        $standaloneManifest = @(Get-Manifest (Join-Path $evidence 'standalone-dashboard'))
        Assert-EqualManifest (Get-PayloadManifest $first) $packageManifest 'Prepared Dashboard / NuGet tools'
        Assert-EqualManifest $packageManifest $standaloneManifest 'Dashboard NuGet tools / standalone archive'
        Save-Manifest $packageManifest 'package-dashboard.json'
        $cliDirectory = Join-Path $evidence 'cli'
        $null = New-Item -ItemType Directory -Force -Path $cliDirectory
        if ($exeSuffix) {
            [IO.Compression.ZipFile]::ExtractToDirectory($cliArchive[0].FullName, $cliDirectory)
        }
        else {
            & tar -xzf $cliArchive[0].FullName -C $cliDirectory
            if ($LASTEXITCODE -ne 0) { throw 'Native CLI archive extraction failed.' }
        }
        $cli = Join-Path $cliDirectory "aspire$exeSuffix"
        if (-not (Test-Path -LiteralPath $cli -PathType Leaf)) { throw 'Native CLI executable is missing.' }
        $installed = Join-Path $evidence 'installed'
        $priorLayout = $env:ASPIRE_LAYOUT_PATH
        try {
            # Setup's post-flip check uses normal layout discovery. Its documented
            # non-default install path therefore also needs ASPIRE_LAYOUT_PATH.
            $env:ASPIRE_LAYOUT_PATH = $installed
            & $cli setup --install-path $installed --force
            if ($LASTEXITCODE -ne 0) { throw 'Actual native CLI embedded payload extraction failed.' }
        }
        finally {
            $env:ASPIRE_LAYOUT_PATH = $priorLayout
            $cliLogs = Join-Path $HOME '.aspire/logs'
            if (Test-Path -LiteralPath $cliLogs) {
                Copy-Item -LiteralPath $cliLogs -Destination (Join-Path $evidence 'cli-diagnostics') -Recurse -Force
            }
        }
        $bundle = Join-Path $installed 'bundle'
        $dashboard = Join-Path $bundle 'dashboard'
        Assert-WindowsMachine $dashboard
        $bundleManifest = @(Get-Manifest $bundle)
        $dashboardManifest = @(Get-Manifest $dashboard)
        Assert-EqualManifest $packageManifest $dashboardManifest 'Dashboard package / actual CLI embedded Dashboard'
        Save-Manifest $bundleManifest 'complete-embedded-bundle.json'
        $tar = Join-Path $repo "artifacts/bundle/aspire-ci-bundlepayload-$Rid.tar.gz"
        $tarDirectory = Join-Path $evidence 'tar-payload'
        $null = New-Item -ItemType Directory -Force -Path $tarDirectory
        & tar -xzf $tar -C $tarDirectory
        if ($LASTEXITCODE -ne 0) { throw 'Prepared payload archive is missing or cannot be extracted.' }
        Assert-EqualManifest (Get-Manifest (Join-Path $tarDirectory $Rid)) $bundleManifest 'Complete tar payload / native CLI embedded payload'
        Save-Manifest (Get-Manifest artifacts/input) 'complete-input-artifacts.json'
        [pscustomobject]@{
            productionBase = '9c281bfde08884fee3c6d42d66139c4ad9e481ed'
            testedHead = (& git --no-pager rev-parse HEAD)
            rid = $Rid
            runtimeArchitecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
            cliVersion = (& $cli --version)
            officialSigning = 'Unvalidated: official pipeline authorization unavailable.'
        } | ConvertTo-Json | Set-Content (Join-Path $evidence 'identity.json')
        "ASPIRE_NATIVE_DASHBOARD_PATH=$(Join-Path $dashboard "Aspire.Dashboard$exeSuffix")" >> $env:GITHUB_ENV
    }
    'Smoke' {
        $dashboard = $env:ASPIRE_NATIVE_DASHBOARD_PATH
        if (-not (Test-Path -LiteralPath $dashboard -PathType Leaf)) { throw 'Extracted native Dashboard is missing.' }
        $smoke = Join-Path $evidence 'native-smoke'
        $data = Join-Path $smoke 'data'
        $null = New-Item -ItemType Directory -Force -Path $data
        $stdout = Join-Path $smoke 'stdout.log'
        $stderr = Join-Path $smoke 'stderr.log'
        $process = Start-Process -FilePath $dashboard -PassThru `
            -WorkingDirectory (Split-Path $dashboard -Parent) `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr `
            -ArgumentList @('--ASPNETCORE_URLS=http://127.0.0.1:0',
                '--ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true',
                '--Dashboard:Otlp:HttpEndpointUrl=http://127.0.0.1:0',
                '--Dashboard:Otlp:GrpcEndpointUrl=http://127.0.0.1:0',
                '--Dashboard:Otlp:AuthMode=Unsecured', '--Dashboard:Api:AuthMode=Unsecured',
                '--Dashboard:Data:PersistenceMode=Run', "--Dashboard:Data:Directory=$data")
        try {
            $deadline = [DateTime]::UtcNow.AddMinutes(1)
            $frontend = $null
            $otlp = $null
            while ([DateTime]::UtcNow -lt $deadline) {
                if ($process.HasExited) { throw "Dashboard exited: $(Get-Content $stderr -Raw)" }
                # Kestrel prints "Now listening on: http://127.0.0.1:54321";
                # the separate OTLP listener prints "OTLP/HTTP listening on: http://127.0.0.1:54322".
                $log = Get-Content $stdout -Raw
                if ($log -match '(?m)^\s*Now listening on:\s+(http://\S+)') { $frontend = $Matches[1] }
                if ($log -match '(?m)^\s*OTLP/HTTP listening on:\s+(http://\S+)') { $otlp = $Matches[1] }
                if ($frontend -and $otlp) { break }
                Start-Sleep -Milliseconds 250
            }
            if (-not $frontend -or -not $otlp) { throw 'Dashboard frontend/OTLP startup timed out.' }
            foreach ($path in @('/', '/_framework/blazor.web.js')) {
                $response = Invoke-WebRequest "$frontend$path" -TimeoutSec 15
                if ($response.StatusCode -ne 200 -or $response.RawContentLength -eq 0) { throw "Dashboard did not serve $path." }
            }
            # Use the OTLP/JSON trace shape covered by Integration/OtlpHttpJsonTests.cs.
            $start = ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000000).ToString()
            $body = @{ resourceSpans = @(@{
                resource = @{ attributes = @(@{ key = 'service.name'; value = @{ stringValue = 'shared-bundle-validation' } }) }
                scopeSpans = @(@{ scope = @{ name = 'shared-bundle-validation' }; spans = @(@{
                    traceId = '5B8EFFF798038103D269B633813FC60C'; spanId = 'EEE19B7EC3C1B174'
                    name = 'shared-bundle-span'; startTimeUnixNano = $start
                    endTimeUnixNano = ([long]$start + 1000000).ToString(); kind = 2
                }) })
            }) } | ConvertTo-Json -Depth 12
            $null = Invoke-RestMethod "$otlp/v1/traces" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 15
            $found = $false
            $deadline = [DateTime]::UtcNow.AddSeconds(30)
            while ([DateTime]::UtcNow -lt $deadline) {
                $response = Invoke-WebRequest "$frontend/api/telemetry/spans" -TimeoutSec 5
                if ($response.Content -like '*shared-bundle-span*') {
                    $response.Content | Set-Content (Join-Path $smoke 'stored-span.json')
                    $found = $true
                    break
                }
                Start-Sleep -Milliseconds 250
            }
            if (-not $found -or @(Get-ChildItem $data -Recurse -File -Filter '*.db').Count -eq 0) {
                throw 'Dashboard did not store and return the submitted span with a SQLite database.'
            }
            Write-Host 'Native Dashboard HTTP/static assets/SQLite/OTLP smoke passed.'
        }
        finally {
            if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force; $process.WaitForExit() }
        }
    }
}
