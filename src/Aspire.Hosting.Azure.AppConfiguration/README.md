# Aspire.Hosting.Azure.AppConfiguration library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure App Configuration.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the .NET Aspire Azure App Configuration Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AppConfiguration
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an App Configuration connection and consume the connection using the following methods:

```csharp
var appConfig = builder.AddAzureAppConfiguration("config");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(appConfig);
```

> NOTE: Consider setting the name of your resource to something other than "config" or "appconfig". Even though durnig deployment random suffix will be added it is still possible to get a name collision.

## Additional documentation

* https://learn.microsoft.com/azure/azure-app-configuration/

## Feedback & contributing

https://github.com/dotnet/aspire
