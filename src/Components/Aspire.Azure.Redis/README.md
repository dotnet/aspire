# Aspire.Azure.Redis library

Configures a `StackExchange.Redis.ConfigurationOptions` in the DI container for connecting to an Azure Cache for [Redis](https://redis.io/) resource. Uses that IConnectionMultiplexer in [ASP.NET Core Output Caching](https://learn.microsoft.com/aspnet/core/performance/caching/output) by using:

- A Service Principal
- A System Assigned Managed Identity
- A User Assigned Managed Identity

## Getting started

### Prerequisites

- Azure Cache Redis resource and Azure Active Directory object such as [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview) or [Service Principal](https://learn.microsoft.com/azure/active-directory/develop/app-objects-and-service-principals) for connecting a client.

### Install the package

Install the Aspire Azure Redis library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Azure.Redis
```

## Usage Example

Call either of these extension methods after the Redis services and configuration have been registered:

- `ConfigureAzureRedisServicePrincipal(clientId, principalId, tenantId, secret)`
- `ConfigureAzureRedisSystemAssignedManagedIdentity(principalId)`
- `ConfigureAzureRedisUserAssignedManagedIdentity(clientId, principalId)`

Example:

```cs
// Add services to the container.
builder.Services.AddRazorPages();

builder.AddRedisOutputCache();

await builder.ConfigureAzureRedisSystemAssignedManagedIdentity("[YOUR_SECRET]");
```

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
