# Microsoft.Extensions.ServiceDiscovery.Yarp

The `Microsoft.Extensions.ServiceDiscovery.Yarp` library adds support for resolving endpoints for YARP clusters, by implementing a [YARP destination resolver](https://github.com/microsoft/reverse-proxy/blob/main/docs/docfx/articles/destination-resolvers.md).

## Usage

### Resolving YARP cluster destinations using Service Discovery

The `IReverseProxyBuilder.AddServiceDiscoveryDestinationResolver()` extension method configures a [YARP destination resolver](https://github.com/microsoft/reverse-proxy/blob/main/docs/docfx/articles/destination-resolvers.md). To use this method, you must also configure YARP itself as described in the YARP documentation, and you must configure .NET Service Discovery via the _Microsoft.Extensions.ServiceDiscovery_ library.

### Direct HTTP forwarding using Service Discovery Forwarding HTTP requests using `IHttpForwarder`

YARP supports _direct forwarding_ of specific requests using the `IHttpForwarder` interface. This, too, can benefit from service discovery using the _Microsoft.Extensions.ServiceDiscovery_ library. To take advantage of service discovery when using YARP Direct Forwarding, use the `IServiceCollection.AddHttpForwarderWithServiceDiscovery` method.

For example, consider the following .NET Aspire application:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure service discovery
builder.Services.AddServiceDiscovery();

// Add YARP Direct Forwarding with Service Discovery support
builder.Services.AddHttpForwarderWithServiceDiscovery();

// ... other configuration ...

var app = builder.Build();

// ... other configuration ...

// Map a Direct Forwarder which forwards requests to the resolved "catalogservice" endpoints
app.MapForwarder("/catalog/images/{id}", "http://catalogservice", "/api/v1/catalog/items/{id}/image");

app.Run();
```

In the above example, the YARP Direct Forwarder will resolve the _catalogservice_ using service discovery, forwarding request sent to the `/catalog/images/{id}` endpoint to the destination path on the resolved endpoints.

## Feedback & contributing

https://github.com/dotnet/aspire
