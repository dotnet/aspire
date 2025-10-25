# Aspire.Azure.Data.AppConfiguration library

Registers [ConfigurationClient](https://learn.microsoft.com/dotnet/api/azure.data.appconfiguration.configurationclient) as a singleton in the DI container for connecting to Azure App Configuration. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- An Azure App Configuration resource - [create an App Configuration store](https://learn.microsoft.com/azure/azure-app-configuration/quickstart-aspnet-core-app)

### Install the package

Install the .NET Aspire Azure App Configuration library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.Data.AppConfiguration
```

## Usage example

In the _Program.cs_ file of your project, call the `AddAzureAppConfigurationClient` extension method to register a `ConfigurationClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureAppConfigurationClient("appconfig");
```

You can then retrieve the `ConfigurationClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly ConfigurationClient _client;

public SettingsController(ConfigurationClient client)
{
    _client = client;
}

public async Task<IActionResult> UpdateSetting(string key, string value)
{
    await _client.SetConfigurationSettingAsync(new ConfigurationSetting(key, value));
    return Ok();
}
```

See the [Azure.Data.AppConfiguration documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/appconfiguration/Azure.Data.AppConfiguration/README.md) for examples on using the `ConfigurationClient`.

## Configuration

The .NET Aspire Azure App Configuration library provides multiple options to configure the Azure App Configuration connection based on the requirements and conventions of your project. Note that either an `Endpoint` or a `ConnectionString` is required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureAppConfigurationClient()`:

```csharp
builder.AddAzureAppConfigurationClient("appconfigConnectionName");
```

And then the connection information will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Service URI

The recommended approach is to use a service URI, which works with the `AzureDataAppConfigurationSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "appconfigConnectionName": "https://{appconfig_name}.azconfig.io"
  }
}
```

#### Connection string

Alternatively, an [Azure App Configuration connection string](https://learn.microsoft.com/azure/azure-app-configuration/concept-connection-string) can be used.

```json
{
  "ConnectionStrings": {
    "appconfigConnectionName": "Endpoint=https://{appconfig_name}.azconfig.io;Id={id};Secret={secret}"
  }
}
```

### Use configuration providers

The Azure App Configuration library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureDataAppConfigurationSettings` and `ConfigurationClientOptions` from configuration by using the `Aspire:Azure:Data:AppConfiguration` key. Example `appsettings.json` that configures some of the options:

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
builder.AddAzureAppConfigurationClient("appconfig", settings => settings.DisableHealthChecks = true);
```

You can also setup the [ConfigurationClientOptions](https://learn.microsoft.com/dotnet/api/azure.data.appconfiguration.configurationclientoptions) using the optional `Action<IAzureClientBuilder<ConfigurationClient, ConfigurationClientOptions>> configureClientBuilder` parameter of the `AddAzureAppConfigurationClient` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```csharp
builder.AddAzureAppConfigurationClient("appconfig", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

## AppHost extensions

In your AppHost project, install the Aspire Azure Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.AppConfiguration
```

Then, in the _Program.cs_ file of `AppHost`, add an App Configuration connection and consume the connection using the following methods:

```csharp
var appConfig = builder.AddAzureAppConfiguration("appconfig");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(appConfig);
```

The `AddAzureAppConfiguration` method will add an Azure App Configuration resource to the builder. The `WithReference` method passes that connection information into a connection string named `appconfig` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureAppConfigurationClient("appconfig");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/appconfiguration/Azure.Data.AppConfiguration/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire