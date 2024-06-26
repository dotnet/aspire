param (
    [switch]$FromMain,
    [switch]$Help
)

function Show-Help {
    Write-Output "Usage: .\script.ps1 [-FromMain] [-Help]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -FromMain  Use the latest build from main branch instead of release."
    Write-Output "  -Help      Display this help message and exit."
}

if ($Help) {
    Show-Help
    exit
}

# Default NuGet Feed URL
$nugetUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"

# Update NuGet Feed URL if -FromMain is specified
if ($FromMain) {
    $nugetUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json"
}

# Package name to search for
$packageName = "Microsoft.NET.Sdk.Aspire.Manifest-8.0.100"

# Fetch the feed data
$feedData = Invoke-RestMethod -Uri $nugetUrl

# Extract resources and find the first package's metadata URL using substring comparison
$metadataUrl = $feedData.resources | Where-Object { $_.'@type' -like '*RegistrationsBaseUrl*' } | Select-Object -First 1 -ExpandProperty '@id'

# Fetch the package data
$packageData = Invoke-RestMethod -Uri "$metadataUrl$packageName/index.json"

# Get the latest version
$latestVersion = $packageData.items[-1].upper

# Create the content for the rollback file
$fileContent = @"
{
  `"microsoft.net.sdk.aspire`": `"$latestVersion/8.0.100`"
}
"@

# Write to file
$fileContent | Out-File -FilePath "aspire-rollback.txt"

# Run dotnet workload update command with optional --include-previews parameter
$updateCommand = "dotnet workload update --source $nugetUrl --skip-sign-check --from-rollback-file .\aspire-rollback.txt"
if ($FromMain) {
    $updateCommand += " --include-previews"
}
Invoke-Expression $updateCommand

# Run dotnet workload install command with optional --include-previews parameter
$installCommand = "dotnet workload install aspire --source $nugetUrl --skip-sign-check --from-rollback-file .\aspire-rollback.txt"
if ($FromMain) {
    $installCommand += " --include-previews"
}
Invoke-Expression $installCommand

# Delete the rollback file as it is no longer needed.
Remove-Item "aspire-rollback.txt" -Force

# Output the latest version
if ($FromMain) {
    Write-Output "Installed Latest version of aspire produced from the main branch. Version installed was $latestVersion."
} else {
    Write-Output "Installed Latest version of aspire produced from the release branch. Version installed was $latestVersion."
}
