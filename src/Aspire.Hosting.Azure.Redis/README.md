# Aspire.Hosting.Azure.Redis library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Cache for Redis.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the `Aspire.Hosting.Azure.Redis` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Redis
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, register a Redis server and consume the connection using the following methods:

```csharp
var redis = builder.AddRedis("cache")
                   .AsAzureRedis();

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(redis);
```

The `WithReference` method configures a connection in the `MyService` project named `cache`. In the _Program.cs_ file of `MyService`, the redis connection can be consumed using the client library [Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis):

```csharp
builder.AddRedisClient("cache");
```

## Additional documentation

* https://stackexchange.github.io/StackExchange.Redis/Basics
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*Redis is a registered trademark of Redis Ltd. Any rights therein are reserved to Redis Ltd._
