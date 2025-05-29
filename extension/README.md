# Aspire Visual Studio Code extension

This extension wraps the Aspire CLI to provide a better development experience for Aspire applications in Visual Studio Code.

## Setup

1. Ensure that powershell is installed on your system.
2. Run either `init.cmd` or `init.ps1` in this directory.
3. Ensure the Aspire CLI is installed. You can install the daily build using `dotnet tool install --global aspire.cli --prerelease --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json`

## Running the extension

Open the `extension` directory in a new VS Code workspace, and f5 to run the extension.

### Notes

On Windows, run `vsts-npm-auth -config .npmrc` to set up a personal access token. You will need to do this anytime you get a 401 error when querying packages. If on Linux or macOS, you will need to follow the instructions [here](https://devdiv.visualstudio.com/DevDiv/_artifacts/feed/vs-impl/connect).
