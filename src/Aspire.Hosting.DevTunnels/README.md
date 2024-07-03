# Aspire.Hosting.DevTunnels library

Provides extension methods to enable DevTunnels for resources with endpoints in a .NET Aspire AppHost.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire DevTunnels Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.DevTunnels
```

## Usage example

Then, in the _Program.cs_ file of `ApPhost`, use the `WithDevTunnel(...)` extension method to expose specific endpoints to the Internet via a DevTunnel.

```csharp
var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(redis);
                       .WithDevTunnel("https");
```

## Additional documentation

* https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview

## Feedback & contributing

https://github.com/dotnet/aspire
