# Aspire.Hosting.Garnet library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Cache for Garnet.

## Install the package

In your AppHost project, install the `Aspire.Hosting.Garnet` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Garnet
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, register a Garnet server and consume the connection using the following methods:

```csharp
var garnet = builder.AddGarnet("cache")

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(garnet);
```

The `WithReference` method configures a connection in the `MyService` project named `cache`. In the _Program.cs_ file of `MyService`, the redis connection can be consumed using the client library [Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis):

```csharp
builder.AddRedisClient("cache");
```

## Additional documentation

* https://github.com/microsoft/garnet/blob/main/README.md
* https://stackexchange.github.io/StackExchange.Redis/Basics
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*Garnet MIT License. Copyright (c) Microsoft Corporation.._
