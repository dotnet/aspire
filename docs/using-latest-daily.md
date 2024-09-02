# Set up your machine to use the latest Aspire builds

These instructions will get you set up with the latest build of Aspire. If you just want the last preview release of .NET Aspire, the packages are on nuget.org, and install the latest [Visual Studio 2022 version 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/) for the tooling.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Run the workload installation script

The [workload installation script](./../eng/installLatestFromReleaseBranch.ps1)) will install the latest .NET Aspire workload from the release branch, but it can also install latest from main (latest nightly build) if the `-FromMain` flag is used (`--fromMain` on Linux/macOS).

### Windows

From a powershell prompt, and from the root of the aspire repo, run:

```shell
.\eng\installLatestFromReleaseBranch.ps1 -FromMain
```

### Linux/macOS

From a terminal, and from the root of the aspire repo, run:

```shell
./eng/installLatestFromReleaseBranch.sh --fromMain
```

## Add necessary NuGet feeds

The latest builds are pushed to a special feed, which you need to add:
```sh
dotnet nuget add source --name dotnet8 https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
```

## Create a new Project

Create an empty .NET Aspire project on the command line:
```shell
dotnet new aspire
```

Alternatively, to create a .NET Aspire project using the Starter template:
```shell
dotnet new aspire-starter
```

These will create a `.sln` file and at least two projects.

Assuming the NuGet feed you added above is visible -- for example you added it globally or it's in a NuGet.config in this folder - you can now build that `.sln`
```shell
dotnet restore
dotnet build
```

And then run it (make sure that Docker desktop is started):
```shell
dotnet run --project "<directoryname>.AppHost"
```

Alternatively, if you are using Visual Studio, you can instead create a new Blazor Web App project and check the `Enlist in Aspire orchestration` box while creating it. Then use <kbd>F5</kbd> to debug or <kbd>Ctrl+F5</kbd> to launch without debugging.

## Troubleshooting

Potential issues that may happen when using these scripts:

- If you are also using other workloads locally (for example, Maui or wasm), then you probably will want to update and install those other workloads first, before running these scripts. The reason is that if you run the scripts first, then the commands that update and install Maui or wasm may interfere with the .NET Aspire workload and bump it to a different (newer) version. For this reason, we recommend that you run the scripts last, after you have installed all other workloads that you want to use.
- On Windows, there is a known issue with workloads when using rollback files, where the commands may fail with un-authorized permissions. This issue has been fixed in 8.0.2xx SDK, but if you are using 8.0.1xx SDK you will likely need to run the script as admin.
- On Windows, also keep in mind that the powershell script is not signed, so it may require you to allow running unsigned scripts. You can do this by running `Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser` in powershell.
- On Linux and macOS, you may need to install `jq` utility which is used by the script to parse the json response from the NuGet feed. You can install it using `sudo apt-get install jq` on Ubuntu, or `brew install jq` on macOS.
