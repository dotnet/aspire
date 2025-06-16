[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$DestinationFile,

    $Overwrite = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Convert Overwrite parameter to boolean if it's a string
if ($Overwrite -is [string]) {
    $Overwrite = $Overwrite -in @('true', 'True', 'TRUE', '$True', '$true', '1', 'yes', 'Yes', 'YES')
} elseif ($Overwrite -is [int]) {
    $Overwrite = $Overwrite -ne 0
}

try {
    # Validate parameters
    if (-not (Test-Path $SourceDirectory -PathType Container)) {
        Write-Error "Source directory does not exist: $SourceDirectory"
        exit 1
    }

    # Check if destination file exists and handle overwrite
    if (Test-Path $DestinationFile) {
        if ($Overwrite) {
            Remove-Item $DestinationFile -Force
        } else {
            Write-Error "Destination file already exists and Overwrite is false: $DestinationFile"
            exit 1
        }
    }

    # Ensure destination directory exists
    $destinationDir = Split-Path $DestinationFile -Parent
    if (-not (Test-Path $destinationDir)) {
        New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
    }

    # Use tar command to create the archive
    # Change to the source directory to avoid including the full path in the archive
    Push-Location $SourceDirectory
    try {
        # Create tar.gz archive with all files in the current directory
        $tarArgs = @(
            "-czf"
            $DestinationFile
            "."
        )

        & tar @tarArgs

        if ($LASTEXITCODE -ne 0) {
            throw "tar command failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    exit 0
}
catch {
    Write-Error "Failed to create tar.gz archive: $($_.Exception.Message)"
    exit 1
}
