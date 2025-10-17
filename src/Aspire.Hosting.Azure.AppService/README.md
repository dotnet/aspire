# Aspire.Hosting.Azure.AppService library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure App Service.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure App Service Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AppService
```

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage example

In the _Program.cs_ file of your AppHost project, add an Azure App Service Environment and publish your project as an Azure App Service website:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env");

builder.AddProject<Projects.MyWebApp>("webapp")
    .PublishAsAzureAppServiceWebsite((infrastructure, site) =>
    {
        // Configure the App Service website
        site.SiteConfig.IsWebSocketsEnabled = true;
    });
```

### Publishing compute resources to Azure App Service

The `PublishAsAzureAppServiceWebsite` extension method is used to configure a compute resource (such as a project) to be published as an Azure App Service website when deploying to Azure. This method allows you to customize the App Service website configuration using the Azure Provisioning SDK.

```csharp
builder.AddProject<Projects.MyApi>("api")
    .WithHttpEndpoint()
    .PublishAsAzureAppServiceWebsite((infrastructure, site) =>
    {
        // Customize the App Service website settings
        site.SiteConfig.IsWebSocketsEnabled = true;
        site.SiteConfig.MinTlsVersion = SupportedTlsVersions.Tls1_2;
    });
```

### Adding an Azure App Service Environment

The Azure App Service Environment resource creates the underlying infrastructure needed to host your applications, including:

- An Azure App Service Plan
- An Azure Container Registry for storing container images
- A managed identity for accessing the container registry
- Optionally, the Aspire Dashboard as an Azure App Service website

```csharp
var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env");
```

By default, the Aspire Dashboard is included in the App Service Environment. To disable the dashboard:

```csharp
var appServiceEnvironment = builder.AddAzureAppServiceEnvironment("env");
appServiceEnvironment.Resource.EnableDashboard = false;
```

## Additional documentation

* https://learn.microsoft.com/azure/app-service/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
