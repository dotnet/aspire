# Aspire.Hosting.Azure.WebPubSub library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Web PubSub.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure Web PubSub Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.WebPubSub
```

## Usage example

In the _AppHost.cs_ file of `AppHost`, add a WebPubSub connection and consume the connection using the following methods:

```csharp
var wps = builder.AddAzureWebPubSub("wps1");

var web = builder.AddProject<Projects.WebPubSubWeb>("webfrontend")
                       .WithReference(wps);
```

## Additional documentation

* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
