// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Aspire.OpenAI.Tests;

public class OpenAISettingsTests
{
    [Fact]
    public void MetricsIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureMetricsIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureMetricsIsEnabledWhenAzureSwitchIsSet(false), EnableTelemetry()).Dispose();
    }

    [Fact]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false), EnableTelemetry()).Dispose();
    }

    private static void EnsureMetricsIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.Equal(expectedValue, new OpenAISettings().DisableMetrics);
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.Equal(expectedValue, new OpenAISettings().DisableTracing);
    }

    private static RemoteInvokeOptions EnableTelemetry()
        => new()
        {
            RuntimeConfigurationOptions = { { "OpenAI.Experimental.EnableOpenTelemetry", true } }
        };
}
