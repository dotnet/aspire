// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

public class AzureMessagingEventHubsSettingsTests
{
    [Fact]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true), EnableTracingForAzureSdk()).Dispose();
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        // doesn't matter which concrete class we use, as the property is defined in the base class
        Assert.Equal(expectedValue, new AzureMessagingEventHubsConsumerSettings().Tracing);
    }

    private static RemoteInvokeOptions EnableTracingForAzureSdk()
        => new()
        {
            RuntimeConfigurationOptions = { { "Azure.Experimental.EnableActivitySource", true } }
        };
}
