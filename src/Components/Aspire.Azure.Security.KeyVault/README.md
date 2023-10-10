# Aspire.Azure.Security.KeyVault

Registers a [SecretClient](https://learn.microsoft.com/dotnet/api/azure.security.keyvault.secrets.secretclient) in the DI container for connecting to Azure Key Vault. Enables corresponding health checks, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Key Vault - [create one](https://learn.microsoft.com/azure/key-vault/general/quick-create-portal).

### Install the package

Install the Aspire Azure Key Vault library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Azure.Security.KeyVault
```

## Usage Example

In the `Program.cs` file of your project, call the `AddAzureKeyVaultSecrets` extension to register a `SecretClient` for use via the dependency injection container. The method takes a connection name parameter.

```cs
builder.AddAzureKeyVaultSecrets("secrets");
```

You can then retrieve the `SecretClient` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly SecretClient _client;

public ProductsController(SecretClient client)
{
    _client = client;
}
```

See the [Azure.Security.KeyVault.Secrets documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/keyvault/Azure.Security.KeyVault.Secrets/README.md) for examples on using the `SecretClient`.

## Configuration

The Aspire Azure Key Vault library provides multiple options to configure the Azure Key Vault connection based on the requirements and conventions of your project. Note that the `VaultUri` is required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureKeyVaultSecrets()`:

```cs
builder.AddAzureKeyVaultSecrets("secretConnectionName");
```

And then the vault URI will be retrieved from the `ConnectionStrings` configuration section. The vault URI which works with the `AzureSecurityKeyVaultSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "secretConnectionName": "https://{account_name}.vault.azure.net/"
  }
}
```

### Use configuration providers

The Aspire Azure Key Vault library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureSecurityKeyVaultSettings` and `SecretClientOptions` from configuration by using the `Aspire:Azure:Security:KeyVault` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Security": {
        "KeyVault": {
          "HealthChecks": false,
          "Tracing": true,
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

You can also pass the `Action<AzureSecurityKeyVaultSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```cs
    builder.AddAzureKeyVaultSecrets("secrets", settings => settings.HealthChecks = false);
```

You can also setup the [SecretClientOptions](https://learn.microsoft.com/dotnet/api/azure.security.keyvault.secrets.secretclientoptions) using the optional `Action<IAzureClientBuilder<SecretClient, SecretClientOptions>> configureClientBuilder` parameter of the `AddAzureKeyVaultSecrets` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```cs
    builder.AddAzureKeyVaultSecrets("secrets", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/keyvault/Azure.Security.KeyVault.Secrets/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/aspire
