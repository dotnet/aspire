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

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, register a Redis server and consume the connection using the following methods:

```csharp
var redis = builder.AddAzureRedis("cache");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(redis);
```

The `WithReference` method configures a connection in the `MyService` project named `cache`. By default, `AddAzureRedis` configures [Microsoft Entra ID](https://learn.microsoft.com/azure/azure-cache-for-redis/cache-azure-active-directory-for-authentication) authentication. This requires changes to applications that need to connect to these resources. In the _Program.cs_ file of `MyService`, the redis connection can be consumed using the client library [Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis) and [Microsoft.Azure.StackExchangeRedis](https://www.nuget.org/packages/Microsoft.Azure.StackExchangeRedis):

```csharp
var azureOptionsProvider = new AzureOptionsProvider();
var configurationOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("cache") ?? throw new InvalidOperationException("Could not find a 'cache' connection string."));
if (configurationOptions.EndPoints.Any(azureOptionsProvider.IsMatch))
{
    await configurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());
}

builder.AddRedisClient("cache", configureOptions: options =>
{
    options.Defaults = configurationOptions.Defaults;
});
```

## Additional documentation

* https://stackexchange.github.io/StackExchange.Redis/Basics
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*Redis is a registered trademark of Redis Ltd. Any rights therein are reserved to Redis Ltd._
