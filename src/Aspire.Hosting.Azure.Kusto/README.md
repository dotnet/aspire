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

## Additional documentation

* https://learn.microsoft.com/en-us/kusto/
* https://learn.microsoft.com/en-us/kusto/api/
* https://learn.microsoft.com/en-us/azure/data-explorer/kusto-emulator-overview

## Feedback & contributing

https://github.com/dotnet/aspire
