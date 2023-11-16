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
