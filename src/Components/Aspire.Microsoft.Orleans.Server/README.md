# Aspire.Microsoft.Orleans.Server library

Add and configure an Orleans server in the host.

## Getting started

### Prerequisites

For running locally, there is no prerequisites.

For cloud deployment, you will need to have:
- a clustering provider (Azure Table is the only one supported for now)
- (optional) a grain storage provider (Azure Table and Azure Blob are the only one supported for now)

### Install the package

Install the .NET Aspire Orleans Server library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Microsoft.Orleans.Server
```

## Usage example

In the _Program.cs_ file of your project, call the `UseAspireOrleansServer` extension method to add a silo to the host.

## Configuration

### AppHost

#### Local development only

In your AppHost project, register an Orleans cluster to use the in memory clustering and the in memory grain storage (optional):

```csharp
var orleans = builder.AddOrleans("my-app")
                     .WithLocalhostClustering()
                     .WithInMemoryGrainStorage("Default"); // Optional, if you want to use grain persistence
```

#### Using production clustering provider storage provider

In your AppHost project, register a clustering table and optionally a grain storage:

```csharp
var clusteringTable = storage.AddTables("clustering");
var grainStorage = storage.AddBlobs("grainstate");

var orleans = builder.AddOrleans("my-app")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("Default", grainStorage);
```

The Orleans resource can then be added with `AddResource` like this:

```csharp
builder.AddProject<Projects.OrleansServer>("silo")
       .AddResource(orleans);
```

## Additional documentation

* https://github.com/dotnet/orleans
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

