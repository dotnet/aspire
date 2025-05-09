# Aspire.Hosting.Milvus library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Milvus vector database resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Milvus Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Milvus
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Milvus resource and consume the connection using the following methods:

```csharp
var milvus = builder.AddMilvus("milvus");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(milvus);
```

## Additional documentation
* https://milvus.io/docs

## Feedback & contributing

https://github.com/dotnet/aspire

_*Milvus and the Milvus logo are used with permission from the Milvus project. All rights reserved by LF AI & Data foundation._
