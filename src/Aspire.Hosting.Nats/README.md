# Aspire.Hosting.NATS library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a NATS resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire NATS Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Nats
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a NATS resource and consume the connection using the following methods:

```csharp
var nats = builder.AddNats("nats");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(nats);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/messaging/nats-component

## Feedback & contributing

https://github.com/dotnet/aspire
