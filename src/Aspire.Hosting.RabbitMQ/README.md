# Aspire.Hosting.RabbitMQ library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a RabbitMQ resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire RabbitMQ Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.RabbitMQ
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a RabbitMQ resource and consume the connection using the following methods:

```csharp
var rmq = builder.AddRabbitMQ("rmq");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(rmq);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/messaging/rabbitmq-client-component

## Feedback & contributing

https://github.com/dotnet/aspire
