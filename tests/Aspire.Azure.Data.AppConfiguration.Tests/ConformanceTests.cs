// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.AppConfiguration.Tests;

public class ConformanceTests : ConformanceTests<ConfigurationClient, AzureDataAppConfigurationSettings>
{
    // Authentication method: Azure AD User Account  
    // Roles: App Configuration Data Reader, App Configuration Data Owner
    public const string ServiceEndpoint = "https://aspiretests.azconfig.io";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Data.AppConfiguration.ConfigurationClient";
    protected override string? ConfigurationSectionName => "Aspire:Azure:Data:AppConfiguration";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Data": {
                "AppConfiguration": {
                  "Endpoint": "https://YOUR_ENDPOINT.azconfig.io",
                  "DisableHealthChecks": true,
                  "DisableTracing": false,
                  "ClientOptions": {
                    "EnableTenantDiscovery": true,
                    "Retry": {
                      "Mode": "Exponential",
                      "Delay": "00:00:01"
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
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "YOUR_ENDPOINT"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "https://YOUR_ENDPOINT.azconfig.io", "DisableHealthChecks": "true"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "https://YOUR_ENDPOINT.azconfig.io", "ClientOptions": {"Retry": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "https://YOUR_ENDPOINT.azconfig.io", "ClientOptions": {"Retry": {"NetworkTimeout": "3S"}}}}}}}""", "The string value is not a match for the indicated regular expression")
        };

    protected override string[] RequiredLogCategories => new string[]
    {
        "Azure.Identity",
        "Azure.Core"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:Data:AppConfiguration", key, "Endpoint"), ServiceEndpoint),
            new(CreateConfigKey("Aspire:Azure:Data:AppConfiguration", key, "ClientOptions:Retry:MaxRetries"), "0")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureDataAppConfigurationSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureAppConfigurationClient("appconfig", ConfigureCredentials);
        }
        else
        {
            builder.AddKeyedAzureAppConfigurationClient(key, ConfigureCredentials);
        }

        void ConfigureCredentials(AzureDataAppConfigurationSettings settings)
        {
            if (CanConnectToServer)
            {
                settings.Credential = new DefaultAzureCredential();
            }
            configure?.Invoke(settings);
        }
    }

    protected override void SetHealthCheck(AzureDataAppConfigurationSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetMetrics(AzureDataAppConfigurationSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureDataAppConfigurationSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(ConfigurationClient service)
    {
        // Try to get configuration settings to trigger activity
        var enumerator = service.GetConfigurationSettingsAsync(new SettingSelector()).AsPages().GetAsyncEnumerator();
        try
        {
            _ = enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult();
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key")).Dispose();

    private static bool GetCanConnect()
    {
        ConfigurationClientOptions clientOptions = new();
        clientOptions.Retry.MaxRetries = 0; // don't enable retries (test runs few times faster)
        ConfigurationClient client = new(new Uri(ServiceEndpoint), new DefaultAzureCredential(), clientOptions);

        try
        {
            // Try to list configuration settings to test connectivity
            var enumerator = client.GetConfigurationSettingsAsync(new SettingSelector()).AsPages().GetAsyncEnumerator();
            try
            {
                _ = enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}