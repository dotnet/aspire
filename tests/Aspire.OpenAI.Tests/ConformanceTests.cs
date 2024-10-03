// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using Xunit;

namespace Aspire.OpenAI.Tests;

public class ConformanceTests : ConformanceTests<OpenAIClient, OpenAISettings>
{
    protected const string ConnectionString = "Endpoint=https://api.openai.com/;Key=fake";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "OpenAI": {
                "Endpoint": "http://YOUR_URI",
                "DisableTracing": false,
                "DisableMetrics": false,
                "ClientOptions": {
                    "NetworkTimeout": "00:00:02"
                }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "OpenAI": {"Endpoint": "YOUR_URI"}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "OpenAI": {"Endpoint": "http://YOUR_URI", "DisableTracing": "true"}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override string ActivitySourceName => "OpenAI.ChatClient";

    protected override string[] RequiredLogCategories => [];

    protected override string? ConfigurationSectionName => "Aspire:OpenAI";

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:OpenAI", key, "Key"), "fake")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<OpenAISettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddOpenAIClient("openai", configure);
        }
        else
        {
            builder.AddKeyedOpenAIClient(key, configure);
        }
    }

    [Fact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: null), EnableTelemetry()).Dispose();

    [Fact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key"), EnableTelemetry()).Dispose();

    protected override void SetHealthCheck(OpenAISettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(OpenAISettings options, bool enabled)
        => options.DisableMetrics = !enabled;

    protected override void SetTracing(OpenAISettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void TriggerActivity(OpenAIClient service)
        => service.GetChatClient("dummy").CompleteChat("dummy gpt");

    private static RemoteInvokeOptions EnableTelemetry()
        => new()
        {
            RuntimeConfigurationOptions = { { "OpenAI.Experimental.EnableOpenTelemetry", true } }
        };
}
