# NuGet Feed URL
$nugetUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"

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

# Run dotnet workload update command
dotnet workload update --source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" --skip-sign-check --from-rollback-file .\aspire-rollback.txt

# Run dotnet workload install command
dotnet workload install aspire --source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" --skip-sign-check --from-rollback-file .\aspire-rollback.txt

# Delete the rollback file as it is no longer needed.
Remove-Item "aspire-rollback.txt" -Force

# Output the latest version
Write-Output "Installed Latest version of aspire produced from the release branch. Version installed was $latestVersion."
