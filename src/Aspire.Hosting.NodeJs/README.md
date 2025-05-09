# Aspire.Hosting.Node.js library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Node.js project.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Node.js library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.NodeJs
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Or resource and consume the connection using the following methods:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var frontend = builder.AddNpmApp("frontend", "../NodeFrontend")

builder.Build().Run();
```

## Additional documentation
https://github.com/dotnet/aspire-samples/tree/main/samples/AspireWithJavaScript
https://github.com/dotnet/aspire-samples/tree/main/samples/AspireWithNode

## Feedback & contributing

https://github.com/dotnet/aspire
