# Aspire.Hosting.Azure.Cosmos library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure CosmosDB.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Cosmos DB account - [create a Cosmos DB account](https://learn.microsoft.com/azure/cosmos-db/nosql/how-to-create-account)

### Install the package

In your AppHost project, install the .NET Aspire Azure Cosmos DB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Cosmos
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a Cosmos DB connection and consume the connection using the following methods:

```csharp
var cosmosdb = builder.AddAzureCosmosDB("cdb").AddDatabase("cosmosdb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(cosmosdb);
```

The `AddAzureCosmosDB` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:cosmosdb` config key. The `WithReference` method passes that connection information into a connection string named `cosmosdb` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Aspire.Microsoft.Azure.Cosmos):

```csharp
builder.AddAzureCosmosDBClient("cosmosdb");
```

### Emulator usage

Aspire supports the usage of the Azure Cosmos DB emulator to use the emulator, add the following to your AppHost project:

```csharp
// AppHost
var cosmosdb = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
```

When the AppHost starts up a local container running the Azure CosmosDB will also be started:

```csharp
// Service code
builder.AddAzureCosmosDbClient("cosmos");
```

## Additional documentation

* https://learn.microsoft.com/azure/cosmos-db/nosql/sdk-dotnet-v3
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
