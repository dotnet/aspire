# Aspire Hosting library for RabbitMQ

Provides extension methods and resources to add RabbitMQ to the Aspire AppHost.

## Install the package

In your AppHost project, install the `Aspire.Hosting.RabbitMQ` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.RabbitMQ
```

## Adding RabbitMQ to the AppHost

To add RabbitMQ to the AppHost use the `AddRabbitMQ` method. The first parameter is the Aspire resource name. The second optional parameter
is the port that the RabbitMQ container will listen on. If the port is not provided the port will be randomly assigned.

```csharp
var messaging = builder.AddRabbitMQ("messaging");
```

To use the RabbitMQ resource in a project use the `WithReference` method. The `WithReference` method will inject an environment variable
called `ConnectionStrings__messaging` (where `messaging` matches the name of the Redis resource).

```csharp
builder.AddProject<Projects.InventoryService>("inventoryservice")
        .WithReference(messaging)
```

## Using RabbitMQ with a .NET project

Once the RabbitMQ resource is configured in the AppHost it can be accessed in a .NET project. To use the RabbitMQ resource in a .NET project
add a reference to the `Aspire.RabbitMQ.Client` NuGet package and add the following code to configure client.

```csharp
builder.AddRabbitMQClient("messaging");
```

## Other resources

For more information see the following resources:

- [Aspire RabbitMQ component tutorial](https://learn.microsoft.com/dotnet/aspire/messaging/rabbitmq-client-component)
