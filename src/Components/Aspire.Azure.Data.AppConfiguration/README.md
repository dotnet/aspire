# Aspire.Azure.Data.AppConfiguration

Retrieves configuration settings from Azure App Configuration to use in your application. Registers a [ConfigurationClient](https://learn.microsoft.com/dotnet/api/azure.data.appconfiguration.configurationclient) in the DI container for connecting to Azure App Configuration. Enables corresponding health checks, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure App Configuration - [create one](https://learn.microsoft.com/azure/azure-app-configuration/quickstart-azure-app-configuration-create).

### Install the package

Install the .NET Aspire Azure App Configuration library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.Data.AppConfiguration
```

## Usage examples

### Add App Configuration to configuration

In the _Program.cs_ file of your project, call the `builder.Configuration.AddAzureAppConfiguration` extension method to add key-values from Azure App Configuration to the application's Configuration. The method takes a connection name parameter.

```csharp
builder.Configuration.AddAzureAppConfiguration("appConfig");
```

You can then retrieve a key-value through normal `IConfiguration` APIS. For example, to retrieve a key-value from a Web API controller:

```csharp
public MyController(IConfiguration configuration)
{
    string someValue = configuration["someKey"];
}
```

#### Use feature flags

To use feature flags, install the Feature Management library:

```dotnetcli
dotnet add package Microsoft.FeatureManagement
```

App Configuration will not load feature flags by default. To load feature flags, you can pass the `Action<AzureAppConfigurationOptions> configureOptions` delegate when calling `builder.Configuration.AddAzureAppConfiguration`.

```csharp
builder.Configuration.AddAzureAppConfiguration("appConfig", configureOptions: options => options.UseFeatureFlags());

// Register feature management services
builder.Services.AddFeatureManagement();
```

You can then use `IVariantFeatureManager` to evaluate feature flags in your application:

```csharp
private readonly IVariantFeatureManager _featureManager;

public MyController(IVariantFeatureManager featureManager)
{
    _featureManager = featureManager;
}

[HttpGet]
public async Task<IActionResult> Get()
{
    if (await _featureManager.IsEnabledAsync("NewFeature"))
    {
        return Ok("New feature is enabled!");
    }
    
    return Ok("Using standard implementation.");
}
```

For information about using the Feature Management library, please go to the [documentation](https://learn.microsoft.com/azure/azure-app-configuration/feature-management-dotnet-reference).

### Use the ConfigurationClient

Alternatively, you can use a `ConfigurationClient` to retrieve the configuration settings on demand. In the _Program.cs_ file of your project, call the `AddAzureAppConfigurationClient` extension method to register a `ConfigurationClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureAppConfigurationClient("appConfig");
```

You can then retrieve the `ConfigurationClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly ConfigurationClient _client;

public MyController(ConfigurationClient client)
{
    _client = client;
}
```

See the [Azure.Data.AppConfiguration documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/appconfiguration/Azure.Data.AppConfiguration/README.md) for examples on using the `ConfigurationClient`.

## Configuration

The .NET Aspire Azure App Configuration library provides multiple options to configure the Azure App Configuration connection based on the requirements and conventions of your project. Note that the App Config `Endpoint` is required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureAppConfigurationClient()`:

```csharp
builder.AddAzureAppConfigurationClient("appConfigConnectionName");
```

And then the App Config endpoint will be retrieved from the `ConnectionStrings` configuration section. The App Config store URI which works with the `AzureDataAppConfigurationSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "appConfigConnectionName": "https://{store_name}.azconfig.io"
  }
}
```

### Use configuration providers

The .NET Aspire Azure App Configuration library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureDataAppConfigurationSettings` and `ConfigurationClientOptions` from configuration by using the `Aspire:Azure:Data:AppConfiguration` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Data": {
        "AppConfiguration": {
          "DisableHealthChecks": true,
          "DisableTracing": false,
          "ClientOptions": {
            "Diagnostics": {
              "ApplicationId": "myapp"
            }
          }
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureDataAppConfigurationSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
builder.AddAzureAppConfigurationClient("appConfig", configureSettings: settings => settings.DisableHealthChecks = true);
```

You can also setup the [ConfigurationClientOptions](https://learn.microsoft.com/dotnet/api/azure.data.appconfiguration.configurationclientoptions) using the optional `Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>> configureClientBuilder` parameter of the `AddAzureAppConfigurationClient` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```csharp
builder.AddAzureAppConfigurationClient("appConfig", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

## AppHost extensions

In your AppHost project, install the Aspire Azure App Configuration Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AppConfiguration
```

Then, in the _Program.cs_ file of `AppHost`, add a App Configuration connection and consume the connection using the following methods:

```csharp
// Service registration
var appConfig = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApppConfiguration("appConfig")
    : builder.AddConnectionString("appConfig");

// Service consumption
var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(appConfig);
```

The `AddAzureAppConfiguration` method adds an Azure App Configuration resource to the builder. Or `AddConnectionString` can be used to read connection information from the AppHost's configuration under the `ConnectionStrings:appConfig` config key. The `WithReference` method passes that connection information into a connection string named `appConfig` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureAppConfigurationClient("appConfig");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/appconfiguration/Azure.Data.AppConfiguration/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
