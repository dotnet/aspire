# Aspire.Hosting.Azure.SignalR library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure SignalR.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure SignalR Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.SignalR
```

## Usage example

In the _Program.cs_ file of `AppHost`, add a SignalR connection and consume the connection using the following methods:

```csharp
var signalR = builder.AddAzureSignalR("sr");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(signalR);
```

## Additional documentation

* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
