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

Add a reference to the .NET-based Azure Functions project in your `AppHost` project.

```dotnetcli
dotnet add reference ..\MyAzureFunctionsProject.csproj
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

builder.AddAzureFunctionsProject<Projects.MyAzureFunctionsProject>("MyAzureFunctionsProject")
    .WithReference(queue)
    .WithReference(blob);

var app = builder.Build();

app.Run();
```

> [!NOTE]
> The Azure Functions integration currently only support Azure Storage Queues, Azure Storage Blobs, and Azure Event Hubs as resource dependencies.

## Feedback & contributing

https://github.com/dotnet/aspire
