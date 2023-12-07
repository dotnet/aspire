#!/bin/bash

# Check if jq is installed
if ! command -v jq &> /dev/null
then
    echo "Error: jq is not installed. Please install jq to run this script."
    echo "On Ubuntu/Debian: sudo apt-get install jq"
    echo "On macOS: brew install jq"
    exit 1
fi

# NuGet Feed URL
nugetUrl="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"

# Package name to search for
packageName="Microsoft.NET.Sdk.Aspire.Manifest-8.0.100"

# Fetch the feed data
feedData=$(curl -s $nugetUrl)

# Extract resources and find the first package's metadata URL using jq
metadataUrl=$(echo $feedData | jq -r '.resources[] | select(."@type" | contains("RegistrationsBaseUrl")) | ."@id"' | head -1)

# Fetch the package data
packageData=$(curl -s "${metadataUrl}${packageName}/index.json")

# Get the latest version
latestVersion=$(echo $packageData | jq -r '.items[-1].upper')

# Create the content for the rollback file
fileContent=$(cat <<-END
{
  "microsoft.net.sdk.aspire": "$latestVersion/8.0.100"
}
END
)

# Write to file
echo "$fileContent" > aspire-rollback.txt

# Run dotnet workload update command
dotnet workload update --source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" --skip-sign-check --from-rollback-file ./aspire-rollback.txt

# Run dotnet workload install command
dotnet workload install aspire --source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" --skip-sign-check --from-rollback-file ./aspire-rollback.txt

# Delete the rollback file as it is no longer needed.
rm -f ./aspire-rollback.txt

# Output the latest version
echo "Installed Latest version of aspire produced from the release branch. Version installed was $latestVersion."
