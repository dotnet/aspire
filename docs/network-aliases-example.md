# Container Network Aliases Example

This document demonstrates how to use the `WithNetworkAlias` API to add custom network aliases to container resources.

## Basic Usage

Network aliases allow containers to be discovered via custom DNS names on the Aspire network. By default, containers are accessible using their resource name. You can add additional aliases using the `WithNetworkAlias` extension method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a container with custom network aliases
var redis = builder.AddContainer("redis", "redis")
    .WithNetworkAlias("cache")
    .WithNetworkAlias("redis-server");

builder.Build().Run();
```

In this example:
- The container is accessible via its default name: `redis`
- It's also accessible via the alias: `cache`
- And via the alias: `redis-server`

## Use Case: Database Migration Container

Network aliases are useful when you need to maintain compatibility with existing DNS names or when different services need to refer to the same container using different names:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a PostgreSQL container with multiple aliases for different purposes
var postgres = builder.AddPostgres("postgres")
    .WithNetworkAlias("db")           // Generic alias for database
    .WithNetworkAlias("postgres-db")  // Specific alias
    .WithNetworkAlias("primary-db");  // Indicates primary database role

var catalogDb = postgres.AddDatabase("catalog");

// Services can reference the database using any of these names
builder.AddProject<Projects.CatalogService>("catalog-service")
    .WithReference(catalogDb);

builder.Build().Run();
```

## Use Case: API Gateway with Multiple Names

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// API gateway container accessible via multiple names
var gateway = builder.AddContainer("api-gateway", "nginx")
    .WithNetworkAlias("gateway")
    .WithNetworkAlias("api")
    .WithNetworkAlias("proxy")
    .WithHttpEndpoint(port: 8080);

builder.Build().Run();
```

## Technical Details

- Network aliases are added to the container's network connection in addition to the default alias (the resource name)
- Aliases enable DNS-based service discovery within the Aspire network
- Multiple aliases can be added by calling `WithNetworkAlias` multiple times
- Aliases must be non-empty strings

## API Reference

### WithNetworkAlias Method

```csharp
public static IResourceBuilder<T> WithNetworkAlias<T>(
    this IResourceBuilder<T> builder, 
    string alias) 
    where T : ContainerResource
```

**Parameters:**
- `builder`: The resource builder for the container resource
- `alias`: The network alias for the container (must be non-empty)

**Returns:** The `IResourceBuilder<T>` for chaining additional configuration

**Throws:**
- `ArgumentNullException`: When `builder` or `alias` is null
- `ArgumentException`: When `alias` is empty or whitespace
