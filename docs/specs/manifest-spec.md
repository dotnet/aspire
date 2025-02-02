# Manifest Specification for .NET Aspire's Distributed Application Model

This is a specification for the manifest file for .NET Aspire's Distributed Application Model. The purpose of the manifest file is to allow developers to export definitions of components that comprise their distributed application model and their dependencies so that other tools can process it to facilitate deployment into target runtime environments.

The format of the manifest file itself does not pre-suppose a particular target environment but this document will make reference to specific cloud providers and technologies for illustrative purposes.

## Basic model

The .NET Aspire distributed application model is comprised components which are typically deployed together as a unit. For example there may be a front-end ASP.NET Core application which calls into one or more backend services which in turn may depend on relational databases or caches. Consider the following sample (taken from the eShop light example):

```csharp
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Redis;
using Projects = eShopLite.App.Projects;

var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");
var redis = builder.AddRedisContainer("redis");

var catalog = builder.AddProject<Projects.eShopLite_CatalogService>("catalogservice")
    .WithReference(catalogDb);

var basket = builder.AddProject<Projects.eShopLite_BasketService>("basketservice")
    .WithReference(redis);

builder.AddProject<Projects.eShopLite_Frontend>("frontend")
    .WithServiceReference(basket)
    .WithServiceReference(catalog)
    .IsExternal();

builder.AddContainer("prometheus", "prom/prometheus")
       .WithServiceBinding(9090);

builder.Build().Run();
```

When ```dotnet publish``` is called on the AppHost project containing the code above the application model and dependency projects will be built and the AppHost will be executed in a model which emits an ```aspire-manifest.json``` file in the build artifacts for the AppHost project. The manifest file for the above project would look like the following:

```jsonc
{
    "$schema": "https://json.schemastore.org/aspire-8.0.json",
    "components": {
        "postgres": {
            "type": "postgres.v1"
        },
        "redis": {
            "type": "redis.v1"
        },
        "catalogservice": {
            "type": "project.v1",
            "path": "[relative path to]\\eShopLite.BasketService.csproj",
            "env": {
                "ConnectionStrings__postgres": "{postgres.connectionString}"
            },
            "bindings": {
                "http": {
                    "scheme": "http",
                    "protocol": "tcp",
                    "transport": "http",
                    "external": false
                }
            }
        },
        "basketservice": {
            "type": "project.v1",
            "path": "[relative path to]\\eShopLite.BasketService.csproj",
            "env": {
                "ConnectionStrings__redis": "{redis.connectionString}"
            },
            "bindings": {
                "http": {
                    "scheme": "http",
                    "protocol": "tcp",
                    "transport": "http",
                    "external": false
                }
            }
        },
        "frontend": {
            "type": "project.v1",
            "path": "[relative path to]\\eShopLite.Frontend.csproj",
            "bindings": {
                "https": { // Will end up being bound as external on port 443, container port inferred from container image.
                    "scheme": "https",
                    "protocol": "tcp",
                    "transport": "http",
                    "external": true
                },
                "http": { // Will end up being bound as external on port 80, container port inferred from container image.
                    "scheme": "http",
                    "protocol": "tcp",
                    "transport": "http",
                    "external": true
                }
            }
        },
        "prometheus": {
            "type": "container.v1",
            "image": "prom/prometheus:latest",
            "bindings": {
                "http": {
                    "containerPort": 9090,
                    "protocol": "tcp",
                    "transport": "http",
                    "external": false
                }
            }
        }
    }
}
```
