param(
    # Name of the fuzzing target, see Fuzzers/*.cs files
    [Parameter(Mandatory = $true, Position = 0)]
    [ArgumentCompleter({
            param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
            $corpusSeedPath = Join-Path $PSScriptRoot "Fuzzers"
            if (Test-Path $corpusSeedPath) {
                Get-ChildItem -Path $corpusSeedPath -Filter "$wordToComplete*.cs" | ForEach-Object { $_.BaseName }
            }
        })]
    [string] $Target,

    # Number of parallel jobs to run
    [int] $Jobs,

    # Maximum length of the input
    [int] $MaxLength = 512,

    # Ignore timeouts when running the fuzzer
    [switch] $IgnoreTimeouts,

    # Skip the build of the project useful for reruning the fuzzer without recompiling
    [switch] $NoBuild,

    # Path to the libfuzzer driver
    [string] $LibFuzzer = "libfuzzer-dotnet-windows"
)

$timeout = 30
$SharpFuzz = "sharpfuzz"
$dict = $null

$corpus = Join-Path $PSScriptRoot "corpuses" $Target
$null = New-Item -Path $corpus -ItemType Directory -Force

$CorpusSeed = Join-Path $PSScriptRoot "corpus-seed" $Target

if (Test-Path $CorpusSeed -ErrorAction SilentlyContinue) {
    Write-Output "Copying corpus seed from $CorpusSeed to $corpus"
    Get-ChildItem -Path $CorpusSeed | Copy-Item -Destination $corpus 
}

$project = Join-Path $PSScriptRoot "Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing.csproj"

Set-StrictMode -Version Latest

$outputDir = "bin"
$projectName = (Get-Item $project).BaseName
$projectDll = "$projectName.dll"
$executable = if ($IsWindows) { Join-Path $outputDir "$projectName.exe" }
else { Join-Path $outputDir "$projectName" }

if (!$NoBuild) {

    if (Test-Path $outputDir) {
        Remove-Item -Recurse -Force $outputDir
    }

    dotnet publish $project -c release -o $outputDir

    $exclusions = @(
        "dnlib.dll",
        "SharpFuzz.dll",
        "SharpFuzz.Common.dll",
        $projectDll
    )

    $fuzzingTargets = @(Get-Item "$outputDir/Microsoft.Extensions.ServiceDiscovery.Dns.dll")

    if (($fuzzingTargets | Measure-Object).Count -eq 0) {
        Write-Error "No fuzzing targets found"
        exit 1
    }

    foreach ($fuzzingTarget in $fuzzingTargets) {
        Write-Output "Instrumenting $fuzzingTarget"
        & $SharpFuzz $fuzzingTarget.FullName
    
        if ($LastExitCode -ne 0) {
            Write-Error "An error occurred while instrumenting $fuzzingTarget"
            exit 1
        }
    }
}

$parameters = @(
    "-timeout=$timeout"
)

if ($Jobs) {
    $parameters += "-fork=$Jobs"
}

if ($IgnoreTimeouts) {
    $parameters += "-ignore_timeouts=1"
}

if ($MaxLength) {
    $parameters += "-max_len=$MaxLength"
}

if ($dict) {
    $parameters += "-dict=$dict"
}

& $LibFuzzer @parameters --target_path=$executable --target_arg=$Target $corpus