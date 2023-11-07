# Aspire.RabbitMQ.Client library

Registers an [IConnection](https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IConnection.html) in the DI container for connecting to a RabbitMQ server. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- RabbitMQ server and the server hostname for connecting a client.

### Install the package

Install the .NET Aspire RabbitMQ library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.RabbitMQ.Client
```

## Usage example

In the _Program.cs_ file of your project, call the `AddRabbitMQ` extension method to register an `IConnection` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddRabbitMQ("messaging");
```

You can then retrieve the `IConnection` instance using dependency injection. For example, to retrieve the connection from a Web API controller:

```csharp
private readonly IConnection _connection;

public ProductsController(IConnection connection)
{
    _connection = connection;
}
```

## Configuration

The .NET Aspire RabbitMQ component provides multiple options to configure the connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddRabbitMQ()`:

```csharp
builder.AddRabbitMQ("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "amqp://username:password@localhost:5672"
  }
}
```

See the [ConnectionString documentation](https://www.rabbitmq.com/uri-spec.html) for more information on how to format this connection string.

### Use configuration providers

The .NET Aspire RabbitMQ component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `RabbitMQClientSettings` from configuration by using the `Aspire:RabbitMQ:Client` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "RabbitMQ": {
      "Client": {
        "HealthChecks": false
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<RabbitMQClientSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
    builder.AddRabbitMQ("messaging", settings => settings.HealthChecks = false);
```

You can also setup the [IConnectionFactory](https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IConnectionFactory.html) using the `Action<IConnectionFactory> configureConnectionFactory` delegate parameter of the `AddRabbitMQ` method. For example to set the client provided name for connections:

```csharp
builder.AddRabbitMQ("messaging", configureConnectionFactory: factory => factory.ClientProvidedName = "MyApp");
```

## AppHost extensions

In your AppHost project, register a RabbitMQ container and consume the connection using the following methods:

```csharp
var messaging = builder.AddRabbitMQContainer("messaging");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(messaging);
```

The `WithReference` method configures a connection in the `MyService` project named `messaging`. In the _Program.cs_ file of `MyService`, the RabbitMQ connection can be consumed using:

```csharp
builder.AddRabbitMQ("messaging");
```

## Additional documentation

* https://rabbitmq.github.io/rabbitmq-dotnet-client/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
