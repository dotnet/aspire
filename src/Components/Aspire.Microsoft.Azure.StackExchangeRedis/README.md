# Aspire.Microsoft.Azure.StackExchangeRedis library

Registers [IConnectionMultiplexer](https://stackexchange.github.io/StackExchange.Redis/Basics.html) in the DI container for connecting to Azure Cache for Redis with Azure AD authentication. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- Azure Cache for Redis instance, learn more about how to [Create an Azure Cache for Redis resource](https://learn.microsoft.com/azure/azure-cache-for-redis/quickstart-create-redis).

### Differences with Aspire.StackExchange.Redis

The Aspire.Microsoft.Azure.StackExchangeRedis library is a wrapper around the Aspire.StackExchange.Redis library that provides additional features for connecting to Azure Cache for Redis with Azure AD authentication. If you don't need these features, you can use the Aspire.StackExchange.Redis library instead.

At runtime the client integration will use Azure AD authentication when a credential is configured, allowing passwordless authentication with Azure Cache for Redis.

### Install the package

Install the .NET Aspire Azure StackExchange.Redis library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Microsoft.Azure.StackExchangeRedis
```

## Usage example

In the _Program.cs_ file of your project, use the `AddRedisClientBuilder` extension method with `WithAzureAuthentication` to register a `IConnectionMultiplexer` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddRedisClientBuilder("cache")
    .WithAzureAuthentication();
```

You can then retrieve the `IConnectionMultiplexer` instance using dependency injection. For example, to retrieve the connection from a Web API controller:

```csharp
private readonly IConnectionMultiplexer _cache;

public ProductsController(IConnectionMultiplexer cache)
{
    _cache = cache;
}
```

### Using keyed services

You can also register keyed Redis clients using the keyed services pattern:

```csharp
builder.AddKeyedRedisClientBuilder("cache")
    .WithAzureAuthentication();
```

Then retrieve the keyed service:

```csharp
public ProductsController([FromKeyedServices("cache")] IConnectionMultiplexer cache)
{
    _cache = cache;
}
```

### Adding distributed caching and output caching

You can add distributed caching and output caching to the Redis client by chaining additional methods:

```csharp
builder.AddRedisClientBuilder("cache")
    .WithAzureAuthentication()
    .WithDistributedCache()
    .WithOutputCache();
```

## Configuration

The .NET Aspire Azure StackExchange.Redis component provides multiple options to configure the Redis connection based on the requirements and conventions of your project. Note that at least one host name is required to connect.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddRedisClientBuilder()`:

```csharp
builder.AddRedisClientBuilder("myRedisConnectionName")
    .WithAzureAuthentication();
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myRedisConnectionName": "your_cache_name.redis.cache.windows.net,ssl=true"
  }
}
```

See the [Basic Configuration Settings](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#basic-configuration-strings) of the StackExchange.Redis docs for more information on how to format this connection string.

### Use configuration providers

The Azure StackExchange.Redis component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `StackExchangeRedisSettings` and `ConfigurationOptions` from configuration by using the `Aspire:StackExchange:Redis` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "StackExchange": {
      "Redis": {
        "ConfigurationOptions": {
          "ConnectTimeout": 3000,
          "ConnectRetry": 2
        },
        "DisableHealthChecks": true,
        "DisableTracing": false
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<StackExchangeRedisSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
builder.AddRedisClientBuilder("cache", settings => settings.DisableHealthChecks = true)
    .WithAzureAuthentication();
```

You can also setup the [ConfigurationOptions](https://stackexchange.github.io/StackExchange.Redis/Configuration.html#configuration-options) using the `Action<ConfigurationOptions> configureOptions` delegate parameter of the `AddRedisClientBuilder` method. For example to set the connection timeout:

```csharp
builder.AddRedisClientBuilder("cache", configureOptions: options => options.ConnectTimeout = 3000)
    .WithAzureAuthentication();
```

### Configure Azure AD authentication

Use the `WithAzureAuthentication` method to establish a connection using Azure AD authentication. If no credential is provided, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```csharp
builder.AddRedisClientBuilder("cache")
    .WithAzureAuthentication(new ManagedIdentityCredential());
```

## AppHost extensions

In your AppHost project, install the `Aspire.Hosting.Azure.Redis` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Redis
```

Then, in the _Program.cs_ file of `AppHost`, register an Azure Cache for Redis instance and consume the connection using the following methods:

```csharp
var cache = builder.AddAzureRedis("cache");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(cache);
```

The `WithReference` method configures a connection in the `MyService` project named `cache`. In the _Program.cs_ file of `MyService`, the Redis connection can be consumed using:

```csharp
builder.AddRedisClientBuilder("cache")
    .WithAzureAuthentication();
```

This will also require your Azure environment to be configured by following [these instructions](https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration).

## Additional documentation

* https://stackexchange.github.io/StackExchange.Redis/
* https://github.com/Azure/Microsoft.Azure.StackExchangeRedis
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*Redis is a registered trademark of Redis Ltd. Any rights therein are reserved to Redis Ltd._