// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Azure.Data.AppConfiguration;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.AppConfiguration.Tests;

public class ConformanceTests : ConformanceTests<ConfigurationClient, AzureDataAppConfigurationSettings>
{
    public const string Endpoint = "https://aspiretests.azconfig.io/";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "Azure.Data.AppConfiguration.ConfigurationClient";

    protected override string[] RequiredLogCategories => new string[] { "Azure.Core" };

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Data": {
                "AppConfiguration": {
                  "Endpoint": "http://YOUR_URI",
                  "DisableHealthChecks": true,
                  "DisableTracing": false,
                  "ClientOptions": {
                    "DisableChallengeResourceVerification": true,
                    "Retry": {
                      "Mode": "Exponential",
                      "Delay": "00:03"
                    }
                  }
                }
              }
            }
          }
        }
        """;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new(CreateConfigKey("Aspire:Azure:Data:AppConfiguration", key, "Endpoint"), Endpoint),
            new(CreateConfigKey("Aspire:Azure:Data:AppConfiguration", key, "ClientOptions:Retry:MaxRetries"), "0")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureDataAppConfigurationSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddAzureAppConfigurationClient("appConfig", configure);
        }
        else
        {
            builder.AddKeyedAzureAppConfigurationClient(key, configure);
        }
    }

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "YOUR_URI"}}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "http://YOUR_URI", "DisableHealthChecks": "true"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "http://YOUR_URI", "ClientOptions": {"Retry": {"Mode": "Fast"}}}}}}}""", "Value should match one of the values specified by the enum"),
            ("""{"Aspire": { "Azure": { "Data":{ "AppConfiguration": { "Endpoint": "http://YOUR_URI", "ClientOptions": {"Retry": {"NetworkTimeout": "3S"}}}}}}}""", "The string value is not a match for the indicated regular expression")
        };

    protected override void SetHealthCheck(AzureDataAppConfigurationSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(AzureDataAppConfigurationSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureDataAppConfigurationSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(ConfigurationClient service)
        => service.GetConfigurationSettingAsync("*", null);

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key")).Dispose();
}
