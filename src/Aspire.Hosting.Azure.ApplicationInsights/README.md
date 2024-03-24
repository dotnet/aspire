# Aspire.Hosting.Azure.ApplicationInsights library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Application Insights.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the .NET Aspire Azure Application Insights Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.ApplicationInsights
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add an Application Insights connection and consume the connection using the following methods:

```csharp
var appInsights = builder.AddAzureApplicationInsights("appInsights");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(appInsights);
```

## Additional documentation

* https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview

## Feedback & contributing

https://github.com/dotnet/aspire
