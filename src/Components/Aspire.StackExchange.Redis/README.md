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

Call `AddRedis` extension method to add an `IConnectionMultiplexer` singleton with the desired configurations exposed with `StackExchangeRedisSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `StackExchangeRedisSettings` from configuration by using `Aspire:StackExchange:Redis` key. Note that at least one host name is required to connect. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "ConnectionString": "localhost:6379",
        "ConfigurationOptions": {
          "ConnectTimeout": 5000,
          "ConnectRetry": 2
        },
        "Tracing": false    
      }
    }
  }
}
```
 
 If you have setup your configurations in the `Aspire.StackExchange.Redis` section you can just call the method without passing any parameter.
 
```cs
    builder.AddRedis();
```

If you want to add more than one [IConnectionMultiplexer](https://stackexchange.github.io/StackExchange.Redis/Basics) you could use a named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "INSTANCE_NAME": {
          "ConnectionString": "localhost:6379",
          "ConfigurationOptions": {
            "ConnectTimeout": 5000,
            "ConnectRetry": 2
          },
          "Tracing": false
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddRedis` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddRedis("INSTANCE_NAME");
```

Also you can pass the `Action<StackExchangeRedisSettings>` delegate to set up some or all the options inline, for example to set the `Tracing`:

```cs
    builder.AddRedis(settings => settings.Tracing = false);
```

Here are the configurable options with corresponding default values:

```cs
public sealed class StackExchangeRedisSettings
{
    // A boolean value that indicates whether the Redis health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
}
```

Check [ConfigurationOptions](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#configuration-options) for more info about client config options.

After adding a `IConnectionMultiplexer` to the builder you can get the `IConnectionMultiplexer` singleton instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
