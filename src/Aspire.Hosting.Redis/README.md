# Aspire.Hosting.Redis library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Redis resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Redis Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Redis
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Redis resource and consume the connection using the following methods:

```csharp
var redis = builder.AddRedis("redis");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(redis);
```

## Additional documentation

* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-component
* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-output-caching-component
* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-distributed-caching-component

## Feedback & contributing

https://github.com/dotnet/aspire

_*Redis is a registered trademark of Redis Ltd. Any rights therein are reserved to Redis Ltd._
