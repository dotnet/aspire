# Set up your machine to use the latest Aspire builds

These instructions will get you set up with the latest build of Aspire. If you just want the last preview release of .NET Aspire, the packages are on nuget.org, and install [Visual Studio 2022 version 17.12](https://visualstudio.microsoft.com/vs/preview/) or later for the tooling.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## (Optional) Create a local nuget.config file

Since dogfooding will require using daily build feeds, you may not want to add feeds globally which could alter how other code on your machine builds. To avoid this happening, you can create a local nuget.config file by running the following command in the root of your repository:

```bash
dotnet new nugetconfig
```

## Add necessary NuGet feeds

The latest builds are pushed to a special feed, which you need to add:
```sh
dotnet nuget add source --name dotnet9 https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json
```

If you use [Package Source Mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping), you'll also need to add the following mappings to your NuGet.config

```xml
<packageSourceMapping>
  <packageSource key="dotnet9">
    <package pattern="Aspire.*" />
    <package pattern="Microsoft.Extensions.ServiceDiscovery*" />
    <package pattern="Microsoft.Extensions.Http.Resilience" />
  </packageSource>
</packageSourceMapping>
```

## Install the daily .NET Aspire templates

To be able to create aspire projects, you will need to install the latest Aspire templates. You can do this by running the following command:

```sh
dotnet new install Aspire.ProjectTemplates::*-* --force
```

> [!TIP]
> Release branches are a little different. For example, for the latest build from `release/X.X` branch change the above to be `Aspire.ProjectTemplates::X.X.*-*`. For example, if you want to use the latest build from the `release/9.2` branch, change the above to be `dotnet new install Aspire.ProjectTemplates::9.2.*-* --force`

<!-- break between blocks -->

> [!NOTE]  
> The `--force` parameter is required if you also have the legacy .NET Aspire Workload installed. The new templates have the same name as the old ones, so this command would override those.

## Create a new Project

Create an empty .NET Aspire project on the command line:
```shell
dotnet new aspire
```

Alternatively, to create a .NET Aspire project using the Starter template:
```shell
dotnet new aspire-starter
```

> [!TIP]
> If you get an error saying `Unable to resolve the template, the following installed templates are conflicting`, append a `-9` to the above template names. For example, `dotnet new aspire-starter-9`.

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

## Install the daily CLI

```sh
dotnet tool install --global aspire.cli --prerelease --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json
```
