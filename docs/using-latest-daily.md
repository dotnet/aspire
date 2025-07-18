# Set up your machine to use the latest Aspire builds

If you just want an official release of .NET Aspire, you don't need this document. [The Aspire documentation](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) will get you started.

If you want the latest, unsupported build of Aspire to try, read on.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## (Optional) Create a local nuget.config file

Since this will require using daily build feeds, you may not want to add feeds globally which could alter how other code on your machine builds. To avoid this happening, you can create a local nuget.config file by running the following command in the root of your repository:

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

## Install the daily CLI

On Windows:

```powershell
iex "& { $(irm https://github.com/dotnet/aspire/raw/refs/heads/main/eng/scripts/get-aspire-cli.ps1) } -Quality dev"
```

On Linux, or macOS:

```sh
curl -sSL https://github.com/dotnet/aspire/raw/refs/heads/main/eng/scripts/get-aspire-cli.sh | bash -s -- -q dev
```

<!-- break between blocks -->

## Create a new Project

Create an empty .NET Aspire project on the command line:

```shell
aspire new
```

> [!TIP]
> `aspire new` will automatically update the aspire templates and they will be available in Visual Studio and `dotnet new`.

These will create a `.slnx` file and at least two projects.

Assuming the NuGet feed you added above is visible -- for example you added it globally or it's in a NuGet.config in this folder - you can now run it (make sure that Docker desktop is started):

```shell
aspire run
```

> [!TIP]
> If you see an error attempting to run the application with aspire run, it's likely that you need to update the Aspire packages in the application. You can always use `dotnet run` on the *.AppHost project as a fallback (please report an issue before you do so!)
