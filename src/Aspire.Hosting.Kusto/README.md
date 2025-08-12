# Aspire.Hosting.Kusto library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Kusto emulator resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kusto Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kusto
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Kusto resource and consume the connection using the following methods:

```csharp
var db = builder.AddKusto().RunAsEmulator();

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Feedback & contributing

https://github.com/dotnet/aspire
