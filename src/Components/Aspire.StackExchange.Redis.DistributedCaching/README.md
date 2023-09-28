# Aspire.StackExchange.Redis.DistributedCaching library

Registers an [IDistributedCache](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache) in the DI container that connects to a [Redis](https://redis.io/) server. See [Distributed Caching](https://learn.microsoft.com/aspnet/core/performance/caching/distributed) for more information. Enables corresponding health check, logging, and telemetry.

## Getting started

### Prerequisites

- Redis server and the server hostname for connecting a client.

### Install the package

Install the Aspire StackExchange Redis Distributed Cache library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.StackExchange.Redis.DistributedCaching
```

## Usage Example

In the `Program.cs` file of your project, call the `AddRedisDistributedCache` extension to register an `IDistributedCache` for use via the dependency injection container.

```cs
builder.AddRedisDistributedCache();
```

You can then retrieve the `IDistributedCache` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly IDistributedCache _cache;

public ProductsController(IDistributedCache cache)
{
    _cache = cache;
}
```

## Configuration

The Aspire StackExchange Redis Distributed Cache component provides multiple options to configure the Redis connection based on the requirements and conventions of your project. Note that at least one host name is required to connect.

### Use configuration providers

The Redis Distributed Cache component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `StackExchangeRedisSettings` and `ConfigurationOptions` from configuration by using the `Aspire:StackExchange:Redis` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "ConnectionString": "localhost:6379",
        "ConfigurationOptions": {
          "ConnectTimeout": 3000,
          "ConnectRetry": 2
        },
        "Tracing": false
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<StackExchangeRedisSettings>` delegate to set up some or all the options inline, for example to disable tracing:

```cs
    builder.AddRedisDistributedCache(settings => settings.Tracing = false);
```

You can also setup the [ConfigurationOptions](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#configuration-options) using the `Action<ConfigurationOptions>` delegate, the second parameter of the `AddRedisDistributedCache` method. For example to set the connection timeout:

```cs
    builder.AddRedisDistributedCache(null, options => options.ConnectTimeout = 3000);
```

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
