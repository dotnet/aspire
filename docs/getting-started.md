# Set up your environment

## Install Visual Studio 2022 Internal Preview

1. [Visual Studio 2022 Enterprise IntPreview Setup](https://aka.ms/vs/17/intpreview/vs_enterprise.exe)
    - This channel updates nightly. You need a build from 15-Aug-2023 or later.
2. Set an environment variable `VSEnableAspireOrchestrationParameter=1`
3. Add NuGet sources to apply the following feeds
    - https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet-tools-internal/nuget/v3/index.json
    - https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-grpc-pre-release/nuget/v3/index.json
    - See [Install and manage packages in Visual Studio](https://learn.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources) for instructions.

## Install .NET 8 RC2

1. Add the NuGet feed for .NET 8 - https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json (https://github.com/dotnet/installer#installers-and-binaries)
2. Install the .NET 8 RC2 SDK version 8.0.100-rc.2.23472.8 or newer.
   1. [Windows x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-win-x64.exe)
   2. [Linux x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-linux-x64.tar.gz)
   3. [OSX x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-osx-x64.tar.gz)

## Install Docker Desktop

1. https://www.docker.com/

## Install the Aspire dotnet workload

1. The RC2 SDK is aware that the Aspire workload exists, but the real manifest is not installed by default. In order to install it, you'll need to update the workload in a directory that has a NuGet.config[^3] with the right feeds configured[^2] so that it can pull the latest manifest. Once you have created the NuGet.config file in your working directory, then you need to run the following command[^1]:

   ```shell
    dotnet workload update --skip-sign-check --interactive
   ```

2. The above command will update the Aspire manifest in your RC2 build, meaning it will already be setup for command-line (VS support is coming soon) In-product acquisition (IPA) of the Aspire workload. In order to manually install the workload, you can run the following command[^1]:

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

1. Create a new ASP.NET Core Web App and check the "Enlist in Aspire orchestration" box

2. Change `AppHost\Program.cs` to be:

```C#
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<AppHost.Projects.WebApplication22>(); // USE YOUR APP NAME

await using var app = builder.Build();
return await app.RunAsync();
```
3. <kbd>F5</kbd>

4. Look in the Terminal window to see which port the application is running on

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

# Run the Aspire eShopLite sample

## Enable Azure ServiceBus (optional)

1. Follow [these instructions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#create-a-namespace-in-the-azure-portal) to create a ServiceBus Namespace (pick a unique namespace) and a Queue (explicitly name the Queue "orders").
    - Be sure to follow the "Assign roles to your Azure AD user" and "Launch Visual Studio and sign-in to Azure" sections so the app can authenticate as you.
2. Add the following user-secret to the MyApp orchestrator project (using the unique namespace you created above):

```shell
C:\git\aspire\samples\eShopLite\AppHost> dotnet user-secrets set ConnectionStrings:messaging <serviceBusNamespace>.servicebus.windows.net
```

- You can do the same in VS by right-clicking AppHost in the Solution Explorer -> "Manage User Secrets" and add

```json
{
  "ConnectionStrings": {
    "messaging": "<serviceBusNamespace>.servicebus.windows.net"
  }
}
```

- The `<ServiceBus namespace host>` is labeled in the portal UI as "Host name" e.g. myservicebusinstance.servicebus.windows.net

## Load the Sample Application

1. Make sure Docker Desktop is running
2. Open `Aspire.sln` solution
3. Set `AppHost` project under `\samples` folder to be the startup project. Make sure the launch profile is set to "http".
4. F5, go to http://localhost:15888 and enjoy.
5. When you are done, "Stop Debugging".

# Dashboard

Starting debugging in VS will automatically launch browser with dashboard which is being served at URL "http://localhost:15888" by default. The URL is controlled by launchSettings.json file in AppHost project.

# Tips and known issues

See the [tips and known issues](tips-and-known-issues.md) page.
