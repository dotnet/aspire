# Aspire.Hosting.ValKey library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Cache for ValKey.

## Install the package

In your AppHost project, install the `Aspire.Hosting.ValKey` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.ValKey
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, register a ValKey server and consume the connection using the following methods:

```csharp
var valKey = builder.AddValKey("cache")

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(valKey);
```

The `WithReference` method configures a connection in the `MyService` project named `cache`. In the _Program.cs_ file of `MyService`, the redis connection can be consumed using the client library [Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis):

```csharp
builder.AddRedisClient("cache");
```

## Additional documentation

* https://valkey.io
* https://github.com/valkey-io/valkey/blob/unstable/README.md
* https://stackexchange.github.io/StackExchange.Redis/Basics
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*ValKey MIT License. Copyright (c) Microsoft Corporation.._
