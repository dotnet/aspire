// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Aspire.Azure.AI.Projects.Tests;

public class AzureAIProjectSettingsTests
{
    [Fact]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false), EnableTelemetry()).Dispose();
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.Equal(expectedValue, new AzureAIProjectSettings().DisableTracing);
    }

    private static RemoteInvokeOptions EnableTelemetry()
        => new()
        {
            RuntimeConfigurationOptions = { { "OpenAI.Experimental.EnableOpenTelemetry", true } }
        };
}
