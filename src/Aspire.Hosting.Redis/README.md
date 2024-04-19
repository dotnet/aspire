# Aspire Hosting library for Redis

Provides extension methods and resources to add Redis to the Aspire AppHost.

## Install the package

In your AppHost project, install the `Aspire.Hosting.Redis` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Redis
```

## Adding Redis to the AppHost

To add Redis to the AppHost use the `AddRedis` method. The first parameter is the Aspire resource name. The second optional parameter
is the port that the Redis container will listen on. If the port is not provided the port will be randomly assigned.

```csharp
var cache = builder.AddRedis("cache");
```

To use the Redis resource in a project use the `WithReference` method. The `WithReference` method will inject an environment variable
called `ConnectionStrings__cache` (where `cache` matches the name of the Redis resource).

```csharp
builder.AddProject<Projects.InventoryService>("inventoryservice")
        .WithReference(cache)
```

## Using Redis with a .NET project

Once the Redis resource is configured in the AppHost it can be accessed in a .NET project. To use the Redis resource in a .NET project
add a reference to the `Aspire.StackExchange.Redis` NuGet package and add the following code to configure client.

```csharp
builder.AddRedisClient("cache");
```


## Other resources

For more information see the following resources:

- [Aspire caching components tutorial](https://learn.microsoft.com/dotnet/aspire/caching/caching-components)
- [`Aspire.StackExchange.Redis` component documentation](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-component)
- [`Aspire.StackExchange.Redis.OutputCaching` component documentation](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-output-caching-component)
- [`Aspire.StackExchange.Redis.DistributedCaching` component documentation](https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-distributed-caching-component)
