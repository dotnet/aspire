# Set up your machine to use the latest Aspire builds

These instructions will get you set up with the latest build of Aspire. If you just want the last preview release of .NET Aspire, the packages are on nuget.org, and install the latest [Visual Studio 2022 version 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/) for the tooling.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Add necessary NuGet feeds

The latest builds are pushed to a special feed, which you need to add:
```sh
dotnet nuget add source --name dotnet8 https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
```

As usual this will add the feed to any existing NuGet.config in the directory or above, or else in the global NuGet.config. See [configuring NuGet behavior](https://learn.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior) to read more about that.

Alternatively, if you are using Visual Studio, you can [Install and manage packages in Visual Studio](https://learn.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources) and add the feed `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json` there.

## Install the .NET Aspire dotnet workload

First, we need to make sure you have the latest version of the workload manifest in your sdk. You can do this by running:

```shell
dotnet workload update --skip-sign-check --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
# If you are already on the latest version, then the command is a no-op.
```

Then, we are now able to install the workload with the version of the manifest that we just updated.

```shell
dotnet workload install aspire --skip-sign-check --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
# To update it later if you wish
# dotnet workload update --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
```

Now you are ready to create and run an Aspire app using these latest Aspire components.

## Create a new Project

Create an empty .NET Aspire project on the command line:
```shell
dotnet new aspire
# Alternatively, to create a .NET Aspire project using the Starter template:
# dotnet new aspire-starter
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

## [Optional] Using scripts to install the latest .NET Aspire build from release branches

If you want to install the latest build from the main branch, then you shouldn't follow the next steps, and instead check out: [Add necessary NuGet feeds](#add-necessary-nuget-feeds).

If you want to use the latest .NET Aspire build from release branches, you can use the scripts in this repo to install the latest .NET Aspire build from those. The reason why we provide scripts for builds from release branches but not for main, is that when working with dotnet workloads, it is not easy to specify exactly which version of the workload you want to install, and instead you get the latest NuGet package version that the SDK is able to find in your configured NuGet feeds. For release branches though, this will not work as main branch produces packages like 8.0.0-preview.x, while release branches produce packages like 8.0.0-preview.(x-1). For example, when main produces packages with version 8.0.0-preview.3, release branches produce packages with version 8.0.0-preview.2.

The scripts to install these builds, are [installLatestFromReleaseBranch.ps1](../eng/installLatestFromReleaseBranch.ps1) for Windows, and [installLatestFromReleaseBranch.sh](../eng/installLatestFromReleaseBranch.sh) for Linux and macOS. These scripts will use rollback files, which at this time is the only way to specify which exact version of a workload you want to install. They will query the feed that contains all of the builds from the release branch, get the latest version, and generate a rollback file to be used for the installation. Finally, it will run the workload `update` and `install` commands for you, using the rollback file. Note that these scripts will work even if you already have a different version of the workload installed, independently of whether it is an older or newer version, and it will override it with the one calculated from the script. 

### Troubleshooting

Potential issues that may happen when using these scripts:

- If you are also using other workloads locally (for example, Maui or wasm), then you probably will want to update and install those other workloads first, before running these scripts. The reason is that if you run the scripts first, then the commands that update and install Maui or wasm may interfere with the .NET Aspire workload and bump it to a different (newer) version. For this reason, we recommend that you run the scripts last, after you have installed all other workloads that you want to use.
- On Windows, there is a known issue with workloads when using rollback files, where the commands may fail with un-authorized permissions. This issue has been fixed in 8.0.2xx SDK, but if you are using 8.0.1xx SDK you will likely need to run the script as admin.
- On Windows, also keep in mind that the powershell script is not signed, so it may require you to allow running unsigned scripts. You can do this by running `Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser` in powershell.
- On Linux and macOS, you may need to install `jq` utility which is used by the script to parse the json response from the NuGet feed. You can install it using `sudo apt-get install jq` on Ubuntu, or `brew install jq` on macOS.
