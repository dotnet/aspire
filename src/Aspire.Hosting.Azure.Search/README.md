# Aspire.Hosting.Azure.Search library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure AI Search Service.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure AI Search Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Search
```

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add an Azure AI Search service and consume the connection using the following methods:

```csharp
var search = builder.AddAzureSearch("search");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(search);
```

The `WithReference` method passes that connection information into a connection string named `search` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Search.Documents](https://www.nuget.org/packages/Aspire.Azure.Search.Documents):

```csharp
builder.AddAzureSearchClient("search");
```

## Additional documentation

* https://learn.microsoft.com/azure/search/search-howto-dotnet-sdk
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
