// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AzureOpenAISettingsTests
{
    [Fact]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.Equal(expectedValue, new AzureOpenAISettings().Tracing);
    }
}
