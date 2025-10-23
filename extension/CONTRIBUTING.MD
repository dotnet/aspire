# Set up your machine to contribute

## Install Prerequisites

- Node.js (LTS version)
- NPM and yarn accessible in the PATH
- Visual Studio Code (latest) or Visual Studio Code Insiders
- .NET Aspire CLI must be installed and available in the PATH

## Build CLI

Run `build.ps1` (Windows) or `build.sh` (Mac/Linux) to compile the Aspire CLI, install dependencies, and localize.


## Run extension locally

- Open the extension folder in Visual Studio Code.
- Launch either the `Run Extension` or `Run Extension (cli stop on entry)` launch configuration. The latter will set an environment variable that causes the CLI to wait until a debugger is attached to execute its logic.

### Optional: set the CLI path

If you want to use a just-built version of the CLI, run the `Aspire: Settings` command and set the `Aspire Cli Executable Path` setting to the Aspire CLI output path.

The output path, relative to the Aspire repository root directory, is `artifacts/bin/Aspire.Cli/Debug/net8.0/aspire`.