# Aspire.Hosting.Seq library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Seq resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Seq Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Seq
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Seq resource and consume the connection using the following methods:

```csharp
var seq = builder.AddSeq("seq");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(seq);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/logging/seq-component

## Feedback & contributing

https://github.com/dotnet/aspire
