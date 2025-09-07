# Aspire.Hosting.Kafka library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Kafka resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kafka Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kafka
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Kafka resource and consume the connection using the following methods:

```csharp
var kafka = builder.AddKafka("messaging");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(kafka);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/messaging/kafka-component

## Feedback & contributing

https://github.com/dotnet/aspire
