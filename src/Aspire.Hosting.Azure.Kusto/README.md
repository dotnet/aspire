# Aspire.Hosting.Azure.Kusto library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Kusto resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Azure Kusto Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Kusto
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Kusto resource and consume the connection using the following method:

```csharp
var db = builder.AddAzureKustoCluster("kusto")
                .AddReadWriteDatabase("mydb")
                .RunAsEmulator();

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

The `AddAzureKustoCluster` method will use the default `Standard_D11_v2` SKU with a capacity of 2. For development environments, you may want to use a smaller and more cost-effective SKU like `Dev(No SLA)_Standard_E2a_v4`. You can customize the Kusto cluster configuration using the `ConfigureInfrastructure` method:

```csharp
var kusto = builder.AddAzureKustoCluster("kusto")
                   .ConfigureInfrastructure(infrastructure =>
                   {
                       var cluster = infrastructure.GetProvisionableResources().OfType<KustoCluster>().Single();
                       cluster.Sku = new KustoSku()
                       {
                           Name = KustoSkuName.DevNoSlaStandardE2aV4,
                           Tier = KustoSkuTier.Basic,
                           Capacity = 1
                       };
                   });

var db = kusto.AddReadWriteDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation

* https://learn.microsoft.com/en-us/kusto/
* https://learn.microsoft.com/en-us/kusto/api/
* https://learn.microsoft.com/en-us/azure/data-explorer/kusto-emulator-overview

## Feedback & contributing

https://github.com/dotnet/aspire
