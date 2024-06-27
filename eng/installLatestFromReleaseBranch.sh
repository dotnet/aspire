#!/bin/bash

# Function to display usage information
show_help() {
    echo "Usage: $0 [--fromMain] [--help]"
    echo ""
    echo "Options:"
    echo "  --fromMain  Get the latest build from main branch (default: latest build from the release branch)."
    echo "  --help      Display this help message and exit."
}

# Check if jq is installed
if ! command -v jq &> /dev/null
then
    echo "Error: jq is not installed. Please install jq to run this script."
    echo "On Ubuntu/Debian: sudo apt-get install jq"
    echo "On macOS: brew install jq"
    exit 1
fi

# Default NuGet Feed URL
nugetUrl="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"

# Check for script arguments
fromMain=false
for arg in "$@"; do
    case $arg in
        --fromMain)
            fromMain=true
            nugetUrl="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json"
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $arg"
            show_help
            exit 1
            ;;
    esac
done

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

# Run dotnet workload update command with optional --include-previews parameter
updateCommand="dotnet workload update --source $nugetUrl --skip-sign-check --from-rollback-file ./aspire-rollback.txt"
if [ "$fromMain" = true ]; then
    updateCommand+=" --include-previews"
fi
eval $updateCommand

# Run dotnet workload install command with optional --include-previews parameter
installCommand="dotnet workload install aspire --source $nugetUrl --skip-sign-check --from-rollback-file ./aspire-rollback.txt"
if [ "$fromMain" = true ]; then
    installCommand+=" --include-previews"
fi
eval $installCommand

# Delete the rollback file as it is no longer needed.
rm -f ./aspire-rollback.txt

# Output the latest version
if [ "$fromMain" = true ]; then
    echo "Installed Latest version of .NET Aspire produced from the main branch. Version installed was $latestVersion."
else
    echo "Installed Latest version of .NET Aspire produced from the release branch. Version installed was $latestVersion."
fi
