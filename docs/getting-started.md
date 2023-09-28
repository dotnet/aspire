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
2. [Install the RC2 build](https://github.com/dotnet/installer#table)

## Install Docker Desktop

1. https://www.docker.com/

## Download DCP application orchestrator

1. [Download the orchestrator from here](https://microsoft-my.sharepoint.com/:f:/p/karolz/EoSAlHwu_OVDn3dBWW2D7hUBWOBzON4CeetcWOjiVW4JaQ?e=r5Bqzs)
2. Unzip it into your user profile folder (typically `c:\Users\<your user name>` on Windows). The result should be that you have a ".dcp" folder in your profile, with dcp.exe inside.
    - (and yes, we are working on a better setup story).

# Create a new Project

1. Create a new ASP.NET Core Web App and check the "Enlist in Aspire orchestration" box

2. Change `MyApp\Program.cs` to be:

```C#
var builder = CloudApplication.CreateBuilder(args);

builder.AddProject<MyApp.Projects.WebApplication22>(); // USE YOUR APP NAME

await using var app = builder.Build();
return await app.RunAsync();
```
3. <kbd>F5</kbd>

4. Look in the Terminal window to see which port the application is running on

# Run the Aspire eShopLite sample

## Enable Azure ServiceBus (optional)

1. Follow [these instructions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#create-a-namespace-in-the-azure-portal) to create a ServiceBus Namespace (pick a unique namespace) and a Queue (explicitly name the Queue "orders").
    - Be sure to follow the "Assign roles to your Azure AD user" and "Launch Visual Studio and sign-in to Azure" sections so the app can authenticate as you.
2. Add the following user-secret to the MyApp orchestrator project (using the unique namespace you created above):

```shell
C:\git\aspire\samples\MyApp> dotnet user-secrets set Aspire.Azure.Messaging.ServiceBus:Namespace <ServiceBus namespace host>
```

- You can do the same in VS by right-clicking MyApp in the Solution Explorer -> "Manage User Secrets" and add

```json
{
  "Aspire:Azure:Messaging:ServiceBus:Namespace": "<ServiceBus namespace host>"
}
```

- The `<ServiceBus namespace host>` is labeled in the portal UI as "Host name" and looks similar to "yournamespacename.servicebus.windows.net"

## Load the Sample Application

1. Make sure Docker Desktop is running
2. Open `Aspire.sln` solution
3. Set `MyApp` project under `\samples` folder to be the startup project. Make sure the launch profile is set to "Run Locally".
4. F5, go to http://localhost:5000 and enjoy.
5. When you are done, "Stop Debugging".

# Dashboard

Starting debugging in VS will automatically launch browser with dashboard which is being served at URL "http://localhost:18888" by default. The URL is controlled by launchsettings.json file in MyApp project.

# Tips and known issues

See the [tips and known issues](tips-and-known-issues.md) page.
