# Set up your environment

Follow all steps in [machine-requirements](machine-requirements.md).

## Install the Aspire dotnet workload

### Visual Studio

Ensure you are using the [Visual Studio 2022 Enterprise IntPreview Setup](https://aka.ms/vs/17/intpreview/vs_enterprise.exe) feed.

Check the `ASP.NET and web development` Workload.

Ensure the `.NET Aspire SDK` component is checked in `Individual components`.

### Command line

1. The RC2 SDK is aware that the Aspire workload exists, but the real manifest is not installed by default. In order to install it, you'll need to update the workload in a directory that has a NuGet.config[^3] with the right feeds configured[^2] so that it can pull the latest manifest. Once you have created the NuGet.config file in your working directory, then you need to run the following command[^1]:

   ```shell
    dotnet workload update --skip-sign-check --interactive
   ```

2. The above command will update the Aspire manifest in your RC2 build, meaning it will already be setup for command-line. In-product acquisition (IPA) of the Aspire workload. In order to manually install the workload, you can run the following command[^1]:

   ```shell
    dotnet workload install aspire --skip-sign-check --interactive
   ```

[^1]: The `--skip-sign-check` flag is required because the packages we build out of the Aspire repo are not yet signed.
[^2]: If you want to create a separate NuGet.config instead, these are the contents you need:
      ```xml
      <?xml version="1.0" encoding="utf-8"?>
      <configuration>
      <packageSources>
          <clear />
          <add key="dotnet-tools-internal" value="https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json" />
      </packageSources>
      </configuration>
      ```
[^3]: If you don't want to create a NuGet.config file, you should also be able to run the `update` and `install` commands using the following extra argument: `--source https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json`.

# Create a new Project

1. Create a new Blazor Web App and check the "Enlist in Aspire orchestration" box

2. <kbd>F5</kbd>

3. The dashboard should launch automatically. You can click on your app's "Endpoint" to navigate to the app.

# Create a new Project from the command line using workload templates

- To create an empty Aspire project[^3], run the following command::

```shell
    dotnet new aspire
```

- To create an Aspire project using the Starter template, run the following command:

```shell
    dotnet new aspire-starter
```

[^3]: In order for these commands to work, you must have already installed the Aspire workload by following the steps in #Install-the-Aspire-dotnet-workload section.
