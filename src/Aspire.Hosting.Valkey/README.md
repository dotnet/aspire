# Aspire.Hosting.Valkey library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Cache for Valkey.

## Install the package

In your AppHost project, install the `Aspire.Hosting.Valkey` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Valkey
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, register a Valkey server and consume the connection using the following methods:

```csharp
var valkey = builder.AddValkey("cache")

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(valkey);
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
