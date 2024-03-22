# Aspire.Hosting.Azure.Search library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Search Service.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Search Service - [create an Azure Search Service resource](https://learn.microsoft.com/azure/search/search-create-service-portal)

### Install the package

Install the .NET Aspire Azure Search Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Search
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an Azure Search service and consume the connection using the following methods:

```csharp
var search = builder.AddAzureSearch("search");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(search);
```

The `AddAzureSearch` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:search` config key. The `WithReference` method passes that connection information into a connection string named `search` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Search.Documents](https://www.nuget.org/packages/Aspire.Azure.Search.Documents):

```csharp
builder.AddAzureSearchClient("search");
```

## Additional documentation

* https://learn.microsoft.com/azure/search/search-howto-dotnet-sdk
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
