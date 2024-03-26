# Aspire.Hosting.Azure.Storage library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Storage.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Storage account - [create a storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

### Install the package

Install the .NET Aspire Azure Storage Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Storage
```

## Usage example

In the _Program.cs_ file of `AppHost`, add a Blob (can use tables or queues also) Storage connection and consume the connection using the following methods:

```csharp
var blobs = builder.AddAzureStorage("storage").AddBlobs("blobs");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(blobs);
```

The `AddBlobs` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:blobs` config key. The `WithReference` method passes that connection information into a connection string named `blobs` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Storage.Blobs](https://www.nuget.org/packages/Aspire.Azure.Storage.Blobs):

```csharp
builder.AddAzureBlobClient("blobs");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
