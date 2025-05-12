# Microsoft.Extensions.ServiceDiscovery

The `Microsoft.Extensions.ServiceDiscovery` library is designed to simplify the integration of service discovery patterns in .NET applications. Service discovery is a key component of most distributed systems and microservices architectures. This library provides a straightforward way to resolve service names to endpoint addresses.

In typical systems, service configuration changes over time. Service discovery accounts for by monitoring endpoint configuration using push-based notifications where supported, falling back to polling in other cases. When endpoints are refreshed, callers are notified so that they can observe the refreshed results.

## How it works

Service discovery uses configured _providers_ to resolve service endpoints. When service endpoints are resolved, each registered provider is called in the order of registration to contribute to a collection of service endpoints (an instance of `ServiceEndpointSource`).

Providers implement the `IServiceEndpointProvider` interface. They are created by an instance of `IServiceEndpointProviderProvider`, which are registered with the [.NET dependency injection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection) system.

Developers typically add service discovery to their [`HttpClient`](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient) using the [`IHttpClientFactory`](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory) with the `AddServiceDiscovery` extension method.

Services can be resolved directly by calling `ServiceEndpointResolver`'s `GetEndpointsAsync` method, which returns a collection of resolved endpoints.

### Change notifications

Service configuration can change over time. Service discovery accounts for by monitoring endpoint configuration using push-based notifications where supported, falling back to polling in other cases. When endpoints are refreshed, callers are notified so that they can observe the refreshed results. To subscribe to notifications, callers use the `ChangeToken` property of `ServiceEndpointCollection`. For more information on change tokens, see [Detect changes with change tokens in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/change-tokens?view=aspnetcore-7.0).

### Extensibility using features

Service endpoints (`ServiceEndpoint` instances) and collections of service endpoints (`ServiceEndpointCollection` instances) expose an extensible [`IFeatureCollection`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.ifeaturecollection) via their `Features` property. Features are exposed as interfaces accessible on the feature collection. These interfaces can be added, modified, wrapped, replaced or even removed at resolution time by providers. Features which may be available on a `ServiceEndpoint` include:

* `IHostNameFeature`: exposes the host name of the resolved endpoint, intended for use with [Server Name Identification (SNI)](https://en.wikipedia.org/wiki/Server_Name_Indication) and [Transport Layer Security (TLS)](https://en.wikipedia.org/wiki/Transport_Layer_Security).

### Resolution order

The providers included in the `Microsoft.Extensions.ServiceDiscovery` series of packages skip resolution if there are existing endpoints in the collection when they are called. For example, consider a case where the following providers are registered: _Configuration_, _DNS SRV_, _Pass-through_. When resolution occurs, the providers will be called in-order. If the _Configuration_ providers discovers no endpoints, the _DNS SRV_ provider will perform resolution and may add one or more endpoints. If the _DNS SRV_ provider adds an endpoint to the collection, the _Pass-through_ provider will skip its resolution and will return immediately instead.

## Getting Started

### Installation

To install the library, use the following NuGet command:

```dotnetcli
dotnet add package Microsoft.Extensions.ServiceDiscovery
```

### Usage example

In the _AppHost.cs_ file of your project, call the `AddServiceDiscovery` extension method to add service discovery to the host, configuring default service endpoint providers.

```csharp
builder.Services.AddServiceDiscovery();
```

Add service discovery to an individual `IHttpClientBuilder` by calling the `AddServiceDiscovery` extension method:

```csharp
builder.Services.AddHttpClient<CatalogServiceClient>(c =>
{
  c.BaseAddress = new("https://catalog"));
}).AddServiceDiscovery();
```

Alternatively, you can add service discovery to all `HttpClient` instances by default:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Turn on service discovery by default
    http.AddServiceDiscovery();
});
```

### Resolving service endpoints from configuration

The `AddServiceDiscovery` extension method adds a configuration-based endpoint provider by default.
This provider reads endpoints from the [.NET Configuration system](https://learn.microsoft.com/dotnet/core/extensions/configuration).
The library supports configuration through `appsettings.json`, environment variables, or any other `IConfiguration` source.

Here is an example demonstrating how to configure endpoints for the service named _catalog_ via `appsettings.json`:

```json
{
  "Services": {
    "catalog": {
      "https": [
        "https://localhost:8443",
        "https://10.46.24.90:443"
      ]
    }
  }
}
```

The above example adds two endpoints for the service named _catalog_: `https://localhost:8443`, and `"https://10.46.24.90:443"`.
Each time the _catalog_ is resolved, one of these endpoints will be selected.

If service discovery was added to the host using the `AddServiceDiscoveryCore` extension method on `IServiceCollection`, the configuration-based endpoint provider can be added by calling the `AddConfigurationServiceEndpointProvider` extension method on `IServiceCollection`.

### Configuration

The configuration provider is configured using the `ConfigurationServiceEndpointProviderOptions` class, which offers these configuration options:

* **`SectionName`**: The name of the configuration section that contains service endpoints. It defaults to `"Services"`.

* **`ShouldApplyHostNameMetadata`**: A delegate used to determine if host name metadata should be applied to resolved endpoints. It defaults to a function that returns `false`.

To configure these options, you can use the `Configure` extension method on the `IServiceCollection` within your application's startup class or main program file:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ConfigurationServiceEndpointProviderOptions>(options =>
{
    options.SectionName = "MyServiceEndpoints";

    // Configure the logic for applying host name metadata
    options.ShouldApplyHostNameMetadata = endpoint =>
    {
        // Your custom logic here. For example:
        return endpoint.Endpoint is DnsEndPoint dnsEp && dnsEp.Host.StartsWith("internal");
    };
});
```

This example demonstrates setting a custom section name for your service endpoints and providing a custom logic for applying host name metadata based on a condition.

## Scheme selection when resolving HTTP(S) endpoints

It is common to use HTTP while developing and testing a service locally and HTTPS when the service is deployed. Service Discovery supports this by allowing for a priority list of URI schemes to be specified in the input string given to Service Discovery. Service Discovery will attempt to resolve the services for the schemes in order and will stop after an endpoint is found. URI schemes are separated by a `+` character, for example: `"https+http://basket"`. Service Discovery will first try to find HTTPS endpoints for the `"basket"` service and will then fall back to HTTP endpoints. If any HTTPS endpoint is found, Service Discovery will not include HTTP endpoints.
Schemes can be filtered by configuring the `AllowedSchemes` and `AllowAllSchemes` properties on `ServiceDiscoveryOptions`. The `AllowAllSchemes` property is used to indicate that all schemes are allowed. By default, `AllowAllSchemes` is `true` and all schemes are allowed. Schemes can be restricted by setting `AllowAllSchemes` to `false` and adding allowed schemes to the `AllowedSchemes` property. For example, to allow only HTTPS:

```csharp
services.Configure<ServiceDiscoveryOptions>(options =>
{
  options.AllowAllSchemes = false;
  options.AllowedSchemes = ["https"];
});
```

To explicitly allow all schemes, set the `ServiceDiscoveryOptions.AllowAllSchemes` property to `true`:

```csharp
services.Configure<ServiceDiscoveryOptions>(options => options.AllowAllSchemes = true);
```

## Resolving service endpoints using platform-provided service discovery

Some platforms, such as Azure Container Apps and Kubernetes (if configured), provide functionality for service discovery without the need for a service discovery client library. When an application is deployed to one of these environments, it may be preferable to use the platform's existing functionality instead. The pass-through provider exists to support this scenario while still allowing other provider (such as configuration) to be used in other environments, such as on the developer's machine, without requiring a code change or conditional guards.

The pass-through provider performs no external resolution and instead resolves endpoints by returning the input service name represented as a `DnsEndPoint`.

The pass-through provider is configured by-default when adding service discovery via the `AddServiceDiscovery` extension method.

If service discovery was added to the host using the `AddServiceDiscoveryCore` extension method on `IServiceCollection`, the pass-through provider can be added by calling the `AddPassThroughServiceEndpointProvider` extension method on `IServiceCollection`.

In the case of Azure Container Apps, the service name should match the app name. For example, if you have a service named "basket", then you should have a corresponding Azure Container App named "basket".

## Service discovery in .NET Aspire

.NET Aspire includes functionality for configuring the service discovery at development and testing time. This functionality works by providing configuration in the format expected by the _configuration-based endpoint provider_ described above from the .NET Aspire AppHost project to the individual service projects added to the application model.

Configuration for service discovery is only added for services which are referenced by a given project. For example, consider the following AppHost program:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.CatalogService>("catalog");
var basket = builder.AddProject<Projects.BasketService>("basket");

var frontend = builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basket)
       .WithReference(catalog);
```

In the above example, the _frontend_ project references the _catalog_ project and the _basket_ project. The two `WithReference` calls instruct the .NET Aspire application to pass service discovery information for the referenced projects (_catalog_, and _basket_) into the _frontend_ project.

## Named endpoints

Some services expose multiple, named endpoints. Named endpoints can be resolved by specifying the endpoint name in the host portion of the HTTP request URI, following the format `scheme://_endpointName.serviceName`. For example, if a service named "basket" exposes an endpoint named "dashboard", then the URI `https+http://_dashboard.basket` can be used to specify this endpoint, for example:

```csharp
builder.Services.AddHttpClient<BasketServiceClient>(
    static client => client.BaseAddress = new("https+http://basket"));
builder.Services.AddHttpClient<BasketServiceDashboardClient>(
    static client => client.BaseAddress = new("https+http://_dashboard.basket"));
```

In the above example, two `HttpClient`s are added: one for the core basket service and one for the basket service's dashboard.

### Named endpoints using configuration

With the configuration-based endpoint provider, named endpoints can be specified in configuration by prefixing the endpoint value with `_endpointName.`, where `endpointName` is the endpoint name. For example, consider this _appsettings.json_ configuration which defined a default endpoint (with no name) and an endpoint named "dashboard":

```json
{
  "Services": {
    "basket":
      "https": "https://10.2.3.4:8080", /* the https endpoint, requested via https://basket */
      "dashboard": "https://10.2.3.4:9999" /* the "dashboard" endpoint, requested via https://_dashboard.basket */
    }
  }
}
```

### Named endpoints in .NET Aspire

.NET Aspire uses the configuration-based provider at development and testing time, providing convenient APIs for configuring named endpoints which are then translated into configuration for the target services. For example:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var basket = builder.AddProject<Projects.BasketService>("basket")
    .WithEndpoint(hostPort: 9999, scheme: "https", name: "admin");

var adminDashboard = builder.AddProject<Projects.MyDashboardAggregator>("admin-dashboard")
       .WithReference(basket.GetEndpoint("admin"));

var frontend = builder.AddProject<Projects.Frontend>("frontend")
       .WithReference(basket);
```

In the above example, the "basket" service exposes an "admin" endpoint in addition to the default "http" endpoint which it exposes. This endpoint is consumed by the "admin-dashboard" project, while the "frontend" project consumes all endpoints from "basket". Alternatively, the "frontend" project could be made to consume only the default "http" endpoint from "basket" by using the `GetEndpoint(string name)` method, as in the following example:

```csharp

// The preceding code is the same as in the above sample

var frontend = builder.AddProject<Projects.Frontend>("frontend")
       .WithReference(basket.GetEndpoint("https"));
```

### Named endpoints in Kubernetes using DNS SRV

When deploying to Kubernetes, the DNS SRV service endpoint provider can be used to resolve named endpoints. For example, the following resource definition will result in a DNS SRV record being created for an endpoint named "default" and an endpoint named "dashboard", both on the service named "basket".

```yml
apiVersion: v1
kind: Service
metadata:
  name: basket
spec:
  selector:
    name: basket-service
  clusterIP: None
  ports:
  - name: default
    port: 8080
  - name: dashboard
    port: 8888
```

To configure a service to resolve the "dashboard" endpoint on the "basket" service, add the DNS SRV service endpoint provider to the host builder as follows:

```csharp
builder.Services.AddServiceDiscoveryCore();
builder.Services.AddDnsSrvServiceEndpointProvider();
```

The special port name "default" is used to specify the default endpoint, resolved using the URI `https://basket`.

As in the previous example, add service discovery to an `HttpClient` for the basket service:

```csharp
builder.Services.AddHttpClient<BasketServiceClient>(
    static client => client.BaseAddress = new("https://basket"));
```

Similarly, the "dashboard" endpoint can be targeted as follows:

```csharp
builder.Services.AddHttpClient<BasketServiceDashboardClient>(
    static client => client.BaseAddress = new("https://_dashboard.basket"));
```

### Named endpoints in Azure Container Apps

Named endpoints are not currently supported for services deployed to Azure Container Apps.

## Feedback & contributing

https://github.com/dotnet/aspire
