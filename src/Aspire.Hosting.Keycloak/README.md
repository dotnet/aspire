# Aspire.Hosting.Keycloak library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Keycloak resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Keycloak Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Keycloak
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Keycloak resource and enable service discovery using the following methods:

```csharp
var keycloak = builder.AddKeycloak("keycloak", 8080);

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(keycloak);
```

**Recommendation:** For local development use a stable port for the Keycloak resource (8080 in the example above). It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC tokens (which include the authority URL, with port) beyond the lifetime of the AppHost.

## Deployment to production environments

When deploying Keycloak to production environments that use reverse proxies with TLS termination (such as Azure Container Apps), you need to configure Keycloak to work properly behind the proxy:

```csharp
var keycloak = builder.AddKeycloak("keycloak");

// For deployment behind a reverse proxy (e.g., Azure Container Apps)
if (!builder.ExecutionContext.IsRunMode)
{
    keycloak.WithReverseProxy();
}
```

The `WithReverseProxy()` method configures the following Keycloak environment variables:
- `KC_HTTP_ENABLED=true` - Enables HTTP since the reverse proxy handles TLS termination
- `KC_PROXY_HEADERS=xforwarded` - Configures Keycloak to respect X-Forwarded headers from the reverse proxy
- `KC_HOSTNAME` - Sets the hostname to match the endpoint URL for proper URL generation

This configuration resolves the common deployment issue where Keycloak fails to start with the error: "Key material not provided to setup HTTPS. Please configure your keys/certificates or start the server in development mode."

## Feedback & contributing

https://github.com/dotnet/aspire
