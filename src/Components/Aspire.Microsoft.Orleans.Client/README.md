# Aspire.Microsoft.Orleans.Client library

Add and configure an Orleans client in the host.

## Getting started

### Prerequisites

For running locally, there is no prerequisites.

For cloud deployment, you will need to have:
- a clustering provider (Azure Table is the only one supported for now)

### Install the package

Install the .NET Aspire Orleans Client library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Microsoft.Orleans.Client
```

## Usage example

In the _Program.cs_ file of your project, call the `UseAspireOrleansServer` extension method to add a silo to the host.

## Configuration

### AppHost

#### Local development only

In your AppHost project, register an Orleans cluster to use the in memory clustering and the in memory grain storage (optional):

```csharp
var orleans = builder.AddOrleans("my-app")
                     .WithLocalhostClustering();
```

#### Using production clustering provider storage provider

In your AppHost project, register a clustering table:

```csharp
var clusteringTable = storage.AddTables("clustering");

var orleans = builder.AddOrleans("my-app")
                     .WithClustering(clusteringTable);
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
