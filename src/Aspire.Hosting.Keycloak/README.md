# Aspire.Hosting.Keycloak library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Keycloak resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Keycloak Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Keycloak
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a Keycloak resource and enable service discovery using the following methods:

```csharp
var keycloak = builder.AddKeycloak("keycloak");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(keycloak);
```

## Feedback & contributing

https://github.com/dotnet/aspire
