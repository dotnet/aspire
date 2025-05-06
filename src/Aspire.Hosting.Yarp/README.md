# Aspire.Hosting.Yarp library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a YARP instance.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire YARP Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Yarp
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a YARP resource and provide the configuration file using the following methods:

```csharp
var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                            [...];
var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                            [...];

builder.AddYarp("apigateway")
       .WithConfigFile("yarp.json")
       .WithReference(basketService)
       .WithReference(catalogService);
```

The `yarp.json` configuration file can use the referenced service like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "catalog": {
        "ClusterId": "catalog",
        "Match": {
          "Path": "/catalog/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/catalog" }
        ]
      },
      "basket": {
        "ClusterId": "basket",
        "Match": {
          "Path": "/basket/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/basket" }
        ]
      }
    },
    "Clusters": {
      "catalog": {
        "Destinations": {
          "catalog": {
            "Address": "http://catalogservice",
          }
        }
      },
      "basket": {
        "Destinations": {
          "basket": {
            "Address": "http://basketservice",
          }
        }
      }
    }
  }
}

```

## Additional documentation

* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-component
* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-output-caching-component
* https://learn.microsoft.com/dotnet/aspire/caching/stackexchange-redis-distributed-caching-component

## Feedback & contributing

https://github.com/dotnet/aspire
