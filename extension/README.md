# Aspire Visual Studio Code extension

This extension wraps the Aspire CLI to provide a better development experience for Aspire applications in Visual Studio Code.

## Setup

1. Run `yarn install` in the extension root directory.

## Running the extension

Open the `extension` directory in a new VS Code workspace, and f5 to run the extension. Note that you should have the Aspire CLI installed globally for the extension to work properly. To install the latest nightly CLI build, run `dotnet tool install --global aspire.cli --prerelease --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json`.

## Running tests

To run the tests, you can use the `yarn test` command. This will execute the tests defined in the `src/test` directory. Make sure that you recompile the tests after making changes by running `yarn compile-tests`. If you make changes to the extension code, you can recompile the extension by running `yarn compile`.
