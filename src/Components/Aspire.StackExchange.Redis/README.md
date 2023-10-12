# Aspire.StackExchange.Redis library

Registers an [IConnectionMultiplexer](https://stackexchange.github.io/StackExchange.Redis/Basics) in the DI container for connecting to [Redis](https://redis.io/) server. Enables corresponding health check, logging, and telemetry.

## Getting started

### Prerequisites

- Redis server and the server hostname for connecting a client.

### Install the package

Install the Aspire StackExchange Redis library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.StackExchange.Redis
```

## Usage Example

In the `Program.cs` file of your project, call the `AddRedis` extension method to register an `IConnectionMultiplexer` for use via the dependency injection container. The method takes a connection name parameter.

```cs
builder.AddRedis("cache");
```

You can then retrieve the `IConnectionMultiplexer` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly IConnectionMultiplexer _cache;

public ProductsController(IConnectionMultiplexer cache)
{
    _cache = cache;
}
```

See the [StackExchange.Redis documentation](https://stackexchange.github.io/StackExchange.Redis/Basics) for examples on using the `IConnectionMultiplexer`.

## Configuration

The Aspire StackExchange Redis component provides multiple options to configure the Redis connection based on the requirements and conventions of your project. Note that at least one host name is required to connect.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddRedis()`:

```cs
builder.AddRedis("myRedisConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myRedisConnectionName": "localhost:6379"
  }
}
```

See the [Basic Configuration Settings](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#basic-configuration-strings) of the StackExchange.Redis docs for more information on how to format this connection string.

### Use configuration providers

The Redis component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `StackExchangeRedisSettings` and `ConfigurationOptions` from configuration by using the `Aspire:StackExchange:Redis` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "ConfigurationOptions": {
          "ConnectTimeout": 3000,
          "ConnectRetry": 2
        },
        "HealthChecks": false,
        "Tracing": true
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<StackExchangeRedisSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```cs
builder.AddRedis("cache", settings => settings.HealthChecks = false);
```

You can also setup the [ConfigurationOptions](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#configuration-options) using the `Action<ConfigurationOptions> configureOptions` delegate parameter of the `AddRedis` method. For example to set the connection timeout:

```cs
builder.AddRedis("cache", configureOptions: options => options.ConnectTimeout = 3000);
```

## App Extensions

In your App project, register a Redis container and consume the connection using the following methods:

```cs
var redis = builder.AddRedisContainer("cache");

var myService = builder.AddProject<YourApp.Projects.MyService>()
                       .WithReference(redis);
```

`.WithReference` configures a connection in the `MyService` project named `cache`. In the `Program.cs` file of `MyService`, the redis connection can be consumed using:

```cs
builder.AddRedis("cache");
```

## Additional documentation

* https://stackexchange.github.io/StackExchange.Redis/Basics
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/aspire
