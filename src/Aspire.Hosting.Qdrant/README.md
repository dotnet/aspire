# Aspire.Hosting.Qdrant library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Qdrant vector database resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Qdrant Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Qdrant
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Qdrant resource and consume the connection using the following methods:

```csharp
var qdrant = builder.AddQdrant("qdrant");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(qdrant);
```

## Additional documentation
* https://qdrant.tech/documentation

## Feedback & contributing

https://github.com/dotnet/aspire

_Qdrant, and the Qdrant logo are trademarks or registered trademarks of Qdrant Solutions GmbH of Germany, and used with their permission._
