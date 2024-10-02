# Aspire.Hosting.Azure.Functions library

Provides methods to the .NET Aspire hosting model for Azure functions.

## Getting started

### Prerequisites

* A .NET Aspire project based on the starter template.
* A .NET-based Azure Functions worker project.

### Install the package

In your AppHost project, install the .NET Aspire Azure Functions Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Functions
```

## Usage example

Add the following `PropertyGroup` in your .NET-based Azure Functions project.

```xml
<PropertyGroup>
    <RunCommand>func</RunCommand>
    <RunArguments>start --csharp</RunArguments>
</PropertyGroup>
```

The Aspire Azure Functions integration does not currently support ports configured in the launch profile of the Functions application.
Remove the `commandLineArgs` property in the default `launchSettings.json` file:

```diff
{
  "profiles": {
    "Company.FunctionApp": {
      "commandName": "Project",
-      "commandLineArgs": "--port 7071",
      "launchBrowser": false
    }
  }
}
```

Add a reference to the .NET-based Azure Functions project in your `AppHost` project.

```dotnetcli
dotnet add reference ..\Company.FunctionApp\Company.FunctionApp.csproj
```

In the _Program.cs_ file of `AppHost`, use the `AddAzureFunctionsProject` to configure the Functions project resource.

```csharp
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Functions;

var builder = new DistributedApplicationBuilder();

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

builder.AddAzureFunctionsProject<Projects.Company_FunctionApp>("my-functions-project")
    .WithReference(queue)
    .WithReference(blob);

var app = builder.Build();

app.Run();
```

## Current Limitations

The Azure Functions integration currently only support Azure Storage Queues, Azure Storage Blobs, and Azure Event Hubs as resource references.

The Azure Functions integration does not currently support OpenTelemetry from the locally running Azure Functions host.

Due to a [current bug in the Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools/issues/3594), the Functions host may fail
to find the target project to build:

> Can't determine Project to build. Expected 1 .csproj or .fsproj but found 2

To work around this issue, run the following commands in the Functions project directory:

```dotnetcli
cd Company.FunctionApp
rm bin/ obj/
func start --csharp
```

Then, update the `RunArguments` in the project file as follows:

```diff
<PropertyGroup>
    <RunCommand>func</RunCommand>
-    <RunArguments>start --csharp</RunArguments>
+    <RunArguments>start --no-build --csharp</RunArguments>
</PropertyGroup>
```

Stop the local Functions host running in `Company.FunctionApp` and re-run the Aspire AppHost.

## Feedback & contributing

https://github.com/dotnet/aspire
