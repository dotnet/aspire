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

Call `AddAzureKeyVaultSecrets` extension method to add the `SecretClient` with the desired configurations exposed with `AzureSecurityKeyVaultSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureSecurityKeyVaultSettings` from configuration by using `Aspire:Azure:Security:KeyVault` key. Note that the `VaultUri` is required to be set. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Security": {
        "KeyVault": {
          "VaultUri": "YOUR_VAULT_URI",
          "HealthChecks": true,
          "Tracing": false,
          "ClientOptions": {
            "DisableChallengeResourceVerification": true
          }
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire.Azure.Security.KeyVault` section you can just call the method without passing any parameter.

```cs
    builder.AddAzureKeyVaultSecrets();
```

If you want to add more than one [SecretClient](https://learn.microsoft.com/dotnet/api/azure.security.keyvault.secrets.secretclient) you could use named instances. The json configuration would look like:

```json
{
  "Aspire": {
    "Azure": {
      "Security": {
        "KeyVault": {
          "INSTANCE_NAME": {
            "VaultUri": "YOUR_VAULT_URI",
            "HealthChecks": true,
            "Tracing": false,
            "ClientOptions": {
              "DisableChallengeResourceVerification": true
            }
          }
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddAzureKeyVaultSecrets` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddAzureKeyVaultSecrets("INSTANCE_NAME");
```

Also you can pass the `Action<AzureSecurityKeyVaultSettings>` delegate to set up some or all the options inline, for example to set the `VaultUri`:

```cs
    builder.AddAzureKeyVaultSecrets(settings => settings.VaultUri = new Uri("YOUR_VAULT_URI"));
```

Here are the configurable options with corresponding default values:

```cs
public sealed class AzureSecurityKeyVaultSettings
{
    // A URI to the vault on which the client operates. Appears as "DNS Name" in the Azure portal.
    public Uri? VaultUri { get; set; }

    // The credential used to authenticate to the Azure Key Vault.
    public TokenCredential? Credential { get; set; }

    // A boolean value that indicates whether the Key Vault health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; }
}
```

You can also setup the [SecretClientOptions](https://learn.microsoft.com/dotnet/api/azure.security.keyvault.secrets.secretclientoptions) using `Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>` delegate, the parameter of the `AddAzureKeyVaultSecrets` method. For example to set the `DisableChallengeResourceVerification`:

```cs
    builder.AddAzureKeyVaultSecrets(null, clientBuilder => clientBuilder.ConfigureOptions(options => options.DisableChallengeResourceVerification = true))
```

After adding a `SecretClient` to the builder you can get the `SecretClient` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
