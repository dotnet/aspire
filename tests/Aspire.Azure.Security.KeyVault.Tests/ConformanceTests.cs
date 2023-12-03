// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Security.KeyVault.Tests;

public class ConformanceTests : ConformanceTests<SecretClient, AzureSecurityKeyVaultSettings>
{
    // Roles: Key Vault Secrets User
    public const string VaultUri = "https://aspiretests.vault.azure.net/";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Security.KeyVault.Secrets.SecretClient";

    protected override string[] RequiredLogCategories => new string[] { "Azure.Core" };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string JsonSchemaPath => "src/Components/Aspire.Azure.Security.KeyVault/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Security": {
                "KeyVault": {
                  "VaultUri": "http://YOUR_URI",
                  "HealthChecks": false,
                  "Tracing": true,
                  "ClientOptions": {
                    "DisableChallengeResourceVerification": true,
                    "Retry": {
                      "Mode": "Exponential",
                      "Delay": "PT3S"
                    }
                  }
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "Security":{ "KeyVault": { "VaultUri": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Security":{ "KeyVault": { "VaultUri": "http://YOUR_URI", "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Security":{ "KeyVault": { "VaultUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Security":{ "KeyVault": { "VaultUri": "http://YOUR_URI", "ClientOptions": {"Retry": {"NetworkTimeout": "3S"}}}}}}}""", "Value does not match format \"duration\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:Security:KeyVault", key, "VaultUri"), VaultUri),
            new(CreateConfigKey("Aspire:Azure:Security:KeyVault", key, "ClientOptions:Retry:MaxRetries"), "0")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureSecurityKeyVaultSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureKeyVaultSecrets("secrets", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureKeyVaultSecrets(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureSecurityKeyVaultSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(AzureSecurityKeyVaultSettings settings, bool enabled)
        => settings.HealthChecks = enabled;

    protected override void SetMetrics(AzureSecurityKeyVaultSettings settings, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureSecurityKeyVaultSettings settings, bool enabled)
        => settings.Tracing = enabled;

    protected override void TriggerActivity(SecretClient service)
        => service.GetSecret("IsAlive");

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key")).Dispose();

    private static bool GetCanConnect()
    {
        SecretClientOptions clientOptions = new();
        clientOptions.Retry.MaxRetries = 0; // don't enable retries (test runs few times faster)
        SecretClient secretClient = new(new Uri(VaultUri), new DefaultAzureCredential(), clientOptions);

        try
        {
            return secretClient.GetSecret("IsAlive").Value.Value == "true";
        }
        catch (Exception)
        {
            return false;
        }
    }
}
