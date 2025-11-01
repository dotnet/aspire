# Aspire.Hosting.Azure.Kusto library

Provides extension methods and resource definitions for an Aspire AppHost to configure a Kusto resource.

## Getting started

### Install the package

In your AppHost project, install the Aspire Azure Kusto Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Kusto
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Kusto resource and consume the connection using the following method:

```csharp
var db = builder.AddAzureKustoCluster("kusto")
                .RunAsEmulator()
                .AddReadWriteDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Connection Properties

When you reference Azure Kusto resources using `WithReference`, the following connection properties are made available to the consuming project:

| Property Name | Description |
|---------------|-------------|
| `Uri`         | The cluster endpoint URI, typically `https://<cluster-name>.<region>.kusto.windows.net/` (or the HTTP endpoint when using the emulator) |
| `Azure`       | Indicates this is an Azure resource. This is `true` for Azure clusters and `false` when running the emulator |

Aspire exposes each property as an environment variable named `[RESOURCE]_[PROPERTY]`. For instance, the `Uri` property of a resource called `db1` becomes `DB1_URI`.

## Additional documentation

* https://learn.microsoft.com/en-us/kusto/
* https://learn.microsoft.com/en-us/kusto/api/
* https://learn.microsoft.com/en-us/azure/data-explorer/kusto-emulator-overview

## Feedback & contributing

https://github.com/dotnet/aspire
