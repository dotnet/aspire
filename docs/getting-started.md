# Set up your environment

Follow all steps in [machine-requirements](machine-requirements.md).

## Add necessary NuGet feeds

Add NuGet sources to apply the following feeds
- https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json
- https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json (https://github.com/dotnet/installer#installers-and-binaries)
- See [Install and manage packages in Visual Studio](https://learn.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources) for instructions.

### Command line instructions
```sh
dotnet nuget add source --name dotnet-libraries-internal https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json
dotnet nuget add source --name dotnet8 https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
```

## Install the Azure Artifacts Credential Provider for NuGet

See [full setup instructions](https://github.com/microsoft/artifacts-credprovider#setup).

### Installation on Windows

#### Automatic PowerShell script

[PowerShell helper script](https://github.com/microsoft/artifacts-credprovider/blob/master/helpers/installcredprovider.ps1)

- To install netcore, run `installcredprovider.ps1`
  - e.g. `iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"`
- To install both netfx and netcore, run `installcredprovider.ps1 -AddNetfx`. The netfx version is needed for nuget.exe.
  - e.g. `iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) } -AddNetfx"`

### Installation on Linux and Mac

#### Automatic bash script

[Linux or Mac helper script](https://github.com/microsoft/artifacts-credprovider/blob/master/helpers/installcredprovider.sh)

Examples:
- `wget -qO- https://aka.ms/install-artifacts-credprovider.sh | bash`
- `sh -c "$(curl -fsSL https://aka.ms/install-artifacts-credprovider.sh)"`

> Note: this script only installs the netcore version of the plugin. If you need to have it working with mono msbuild, you will need to download the version with both netcore and netfx binaries following the steps in [Manual installation on Linux and Mac](#installation-on-linux-and-mac)

## Setup NuGet Feed

Unless you work for Microsoft, you won't have access to the internal Azure
DevOps feed. We have a private mirror on GitHub you can use instead. To access
it, you need to perform the following steps:

1. [Create a personal access token](https://github.com/settings/tokens/new) for
   your GitHub account with the `read:packages` scope with your desired
   expiration length:
    [<img width="583" alt="image" src="https://user-images.githubusercontent.com/249088/160220117-7e79822e-a18a-445c-89ff-b3d9ca84892f.png">](https://github.com/settings/tokens/new)

1. At the command line, go to the root of the .NET Aspire repo and run the following
   commands to add the package feed to your NuGet configuration, replacing the
   `<YOUR_USER_NAME>` and `<YOUR_TOKEN>` placeholders with the relevant values:
   ```text
   dotnet nuget remove source dotnet-libraries-internal
   dotnet nuget add source -u <YOUR_USER_NAME> -p <YOUR_TOKEN> --name dotnet-libraries-internal "https://nuget.pkg.github.com/dotnet/index.json"
   ```

## Install the .NET Aspire dotnet workload

### Visual Studio

Ensure you are using the [Visual Studio 2022 Enterprise IntPreview Setup](https://aka.ms/vs/17/intpreview/vs_enterprise.exe) feed.

Check the `ASP.NET and web development` Workload.

Ensure the `.NET Aspire SDK` component is checked in `Individual components`.

### Command line

1. The RTM nightly SDK is aware that the .NET Aspire workload exists, but the real manifest is not installed by default. In order to install it, you'll need to update the workload in a directory that has a NuGet.config[^3] with the right feeds configured[^2] so that it can pull the latest manifest. Once you have created the NuGet.config file in your working directory, then you need to run the following command[^1]:

    ```shell
    dotnet workload update --skip-sign-check --interactive
    ```

2. The above command will update the .NET Aspire manifest in your SDK build, meaning it will already be setup for command-line and Visual Studio in-product acquisition (IPA) of the .NET Aspire workload. In order to manually install the workload, you can run the following command[^1]:

    ```shell
    dotnet workload install aspire --skip-sign-check --interactive
    ```

[^1]: The `--skip-sign-check` flag is required because the packages we build out of the .NET Aspire repo are not yet signed.
[^2]: If you want to create a separate NuGet.config instead, these are the contents you need:
      ```xml
      <?xml version="1.0" encoding="utf-8"?>
      <configuration>
      <packageSources>
          <clear />
          <add key="dotnet8" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json" />
          <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
          <add key="dotnet-tools-internal" value="https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json" />
          <add key="nuget" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
      </configuration>
      ```
[^3]: If you don't want to create a NuGet.config file, you should also be able to run the `update` and `install` commands using the following extra argument: `--source https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json`.

# Create a new Project

## In Visual Studio

1. Create a new Blazor Web App and check the "Enlist in Aspire orchestration" box
2. <kbd>F5</kbd>
3. The dashboard should launch automatically. You can click on your app's "Endpoint" to navigate to the app.

## Using command line using workload templates

- To create an empty .NET Aspire project[^3], run the following command::

```shell
    dotnet new aspire
```

- To create an Aspire project using the Starter template, run the following command:

```shell
    dotnet new aspire-starter
```

[^3]: In order for these commands to work, you must have already installed the .NET Aspire workload by following the steps in #Install-the-Aspire-dotnet-workload section.

You need to create a `NuGet.config` file in the root directory of your project with the contents above.
Once that is created you can build with

```shell
   dotnet build
```

And then run with
```shell
   dotnet run --project "$(basename $PWD).AppHost"
```
