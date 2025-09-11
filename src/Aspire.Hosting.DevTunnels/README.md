# Aspire.Hosting.DevTunnels library

Provides extension methods and resource definitions for a .NET Aspire AppHost to expose local application endpoints publicly via a secure Dev Tunnel.  
Dev tunnels are useful for:
* Sharing a running local service (e.g., a Web API) with teammates, mobile devices, or webhooks.
* Testing incoming callbacks from external SaaS systems (GitHub / Stripe / etc.) without deploying.
* Quickly publishing a temporary, TLS‑terminated endpoint during development.

> By default tunnels require authentication. You can selectively enable anonymous (public) access per tunnel or per individual port.

---

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Dev Tunnels Hosting library via NuGet:

```dotnetcli
dotnet add package Aspire.Hosting.DevTunnels
```

---

## Basic usage

### Expose all endpoints on a project

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.WebApp>("web");

var tunnel = builder.AddDevTunnel("mytunnel")
                    .WithReference(web);

builder.Build().Run();
```

### Enable anonymous (public) access

```csharp
var tunnel = builder.AddDevTunnel("publicapi")
                    .WithReference(web)
                    .WithAnonymousAccess();   // Entire tunnel (all ports) can be accessed anonymously
```

### Expose only specific endpoint(s)

```csharp
var web = builder.AddProject<Projects.WebApp>("web");

var tunnel = builder.AddDevTunnel("apitunnel")
                    .WithReference(web.GetEndpoint("api"));  // Only expose the "api" endpoint
```

### Per‑port anonymous access

You can control anonymous access at the port (endpoint) level using `DevTunnelPortOptions`:

```csharp
var api = builder.AddProject<Projects.ApiService>("api");

var tunnel = builder.AddDevTunnel("mixedaccess")
                    .WithReference(api.GetEndpoint("public"), new DevTunnelPortOptions { AllowAnonymous = true })
                    .WithReference(api.GetEndpoint("admin"));  // This endpoint requires authentication
```

### Custom tunnel ID, description, and labels

```csharp
var options = new DevTunnelOptions
{
    Description = "Shared QA validation tunnel",
    Labels = { "qa", "validation" },
    AllowAnonymous = false
};

var tunnel = builder.AddDevTunnel(
                 name: "qa",
                 tunnelId: "qa-shared",
                 options: options)
             .WithReference(api);
```

### Multiple tunnels for different audiences

```csharp
var web = builder.AddProject<Projects.WebApp>("web");

var publicTunnel = builder.AddDevTunnel("public")
                          .WithReference(web)
                          .WithAnonymousAccess();

var privateTunnel = builder.AddDevTunnel("private")
                           .WithReference(web);  // Requires authentication
```

---

## Service discovery integration

When another resource references a dev tunnel via:

```csharp
builder.AddProject<Projects.ClientApp>("client")
       .WithReference(web, publicTunnel);  // Use the tunneled address for 'web'
```

Environment variables are injected after the tunnel port is allocated using the format:

```
services__{ResourceName}__{EndpointName}__0 = https://{public-host}/
```

Example:

```
services__web__https__0 = https://myweb-1234.westeurope.devtunnels.ms/
```

This lets downstream components use the tunneled address exactly like any other Aspire service discovery entry.

> Referencing a tunnel delays the consumer resource's start until the tunneled endpoint is fully allocated.

---

## Anonymous access options

| Scope            | How to enable                                  | Notes |
|------------------|-------------------------------------------------|-------|
| Entire tunnel    | `tunnel.WithAnonymousAccess()`                  | Affects all ports unless overridden at port level. |
| Specific port(s) | `WithReference(endpoint, new DevTunnelPortOptions { AllowAnonymous = true })` | Fine-grained control per exposed endpoint. |

If neither is set, authentication is required.

---

## Protocol handling

`DevTunnelPortOptions.Protocol` supports:  
* `http`  
* `https`  
* `auto` (let the service decide)  
* `null` (default = use the referenced endpoint's scheme)

Unsupported schemes (e.g., non-HTTP(S)) will throw an exception.

---

## Security considerations

* Prefer authenticated tunnels during normal development.
* Only enable anonymous access for endpoints that are safe to expose publicly.
* Treat public tunnel URLs as temporary & untrusted (rate limit / validate input server-side).

---

## Additional documentation

* [.NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/)
* (Dev Tunnels service documentation – refer to official Microsoft resources as applicable)

---

## Feedback & contributing

https://github.com/dotnet/aspire

Contributions (improvements, clarifications, samples) are welcome.