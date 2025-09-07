# Aspire.Hosting.Yarp library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a YARP reverse proxy instance.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire YARP Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Yarp
```

## Usage examples

### Programmatic configuration

The modern approach uses programmatic configuration with the `WithConfiguration` method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var backendService = builder.AddProject<Projects.Backend>("backend");
var frontendService = builder.AddProject<Projects.Frontend>("frontend");

var gateway = builder.AddYarp("gateway")
                     .WithConfiguration(yarp =>
                     {
                         // Add a catch-all route for the frontend
                         yarp.AddRoute(frontendService);
                         
                         // Add a route with path prefix for the backend API
                         yarp.AddRoute("/api/{**catch-all}", backendService)
                             .WithTransformPathRemovePrefix("/api");
                     });

var app = builder.Build();
await app.RunAsync();
```

### Configuration with external services

You can also route to external services:

```csharp
var externalApi = builder.AddExternalService("external-api", "https://api.example.com");

var gateway = builder.AddYarp("gateway")
                     .WithConfiguration(yarp =>
                     {
                         yarp.AddRoute("/external/{**catch-all}", externalApi)
                             .WithTransformPathRemovePrefix("/external");
                     });
```

### Enabling static file serving

To serve static files alongside proxied routes:

```csharp
var backendService = builder.AddProject<Projects.Backend>("backend");

var gateway = builder.AddYarp("gateway")
                     .WithStaticFiles() // Enables static file serving
                     .WithConfiguration(yarp =>
                     {
                         // API routes are proxied to backend
                         yarp.AddRoute("/api/{**catch-all}", backendService)
                             .WithTransformPathRemovePrefix("/api");
                         
                         // Static files (HTML, CSS, JS, etc.) are served directly
                         // from the YARP container for any non-matching routes
                     });
```

This configuration allows the YARP gateway to serve both static files (for frontend assets) and proxy API requests to backend services.
## Configuration API

### Route configuration

The `IYarpConfigurationBuilder` provides methods to configure routes and clusters:

```csharp
// Add routes with different targets
yarp.AddRoute(resource);                                    // Catch-all route
yarp.AddRoute("/path/{**catch-all}", resource);            // Specific path route
yarp.AddRoute("/path/{**catch-all}", endpoint);            // Route to specific endpoint
yarp.AddRoute("/path/{**catch-all}", externalService);     // Route to external service

// Add clusters directly
var cluster = yarp.AddCluster(resource);
var route = yarp.AddRoute("/path/{**catch-all}", cluster);
```

### Static file serving

To enable static file serving in the YARP container, use the `WithStaticFiles()` method:

```csharp
var gateway = builder.AddYarp("gateway")
                     .WithStaticFiles()  // Enable static file serving
                     .WithConfiguration(yarp =>
                     {
                         yarp.AddRoute("/api/{**catch-all}", backendService)
                             .WithTransformPathRemovePrefix("/api");
                     });
```

This configures the YARP container to serve static files from its default static files directory. The static files will be available at the gateway's base URL while API routes continue to be proxied to backend services.

### Route matching options

Routes can be configured with various matching criteria:

```csharp
yarp.AddRoute("/api/{**catch-all}", backendService)
    .WithMatchMethods("GET", "POST")                        // HTTP methods
    .WithMatchHeaders(new RouteHeader("Content-Type", "application/json"))  // Headers
    .WithMatchHosts("api.example.com")                      // Host header
    .WithOrder(1);                                          // Route priority
```

## Transform extensions

YARP provides various transform extensions to modify requests and responses:

### Path transforms

```csharp
route.WithTransformPathSet("/new/path")                     // Set path
     .WithTransformPathPrefix("/prefix")                    // Add prefix
     .WithTransformPathRemovePrefix("/api")                 // Remove prefix
     .WithTransformPathRouteValues("/users/{id}/posts");    // Use route values
```

### Request header transforms

```csharp
route.WithTransformRequestHeader("X-Forwarded-For", "value")           // Add/set header
     .WithTransformRequestHeaderRouteValue("X-User-Id", "id")          // From route value
     .WithTransformUseOriginalHostHeader(true)                         // Preserve host
     .WithTransformCopyRequestHeaders(false);                          // Copy headers
```

### Response transforms

```csharp
route.WithTransformResponseHeader("X-Powered-By", "Aspire")            // Add response header
     .WithTransformResponseHeaderRemove("Server")                      // Remove header
     .WithTransformCopyResponseHeaders(true);                          // Copy headers
```

### Query parameter transforms

```csharp
route.WithTransformQueryValue("version", "1.0")                       // Add query param
     .WithTransformQueryRouteValue("userId", "id")                     // From route value
     .WithTransformQueryRemoveKey("debug");                            // Remove query param
```

## Advanced configuration

### Multiple routes to the same service

```csharp
builder.AddYarp("gateway")
       .WithConfiguration(yarp =>
       {
           // Different routes to the same backend
           yarp.AddRoute("/api/v1/{**catch-all}", backendService)
               .WithTransformPathRemovePrefix("/api/v1");
               
           yarp.AddRoute("/api/v2/{**catch-all}", backendService)
               .WithTransformPathRemovePrefix("/api/v2")
               .WithTransformPathPrefix("/v2");
       });
```

## Additional documentation

* [YARP documentation](https://microsoft.github.io/reverse-proxy/)
* [.NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/)
* [YARP integration in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/proxies/yarp-integration)
* [Service Discovery in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)

## Feedback & contributing

https://github.com/dotnet/aspire
