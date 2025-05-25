// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class LogAnalyticsWorkspaceTests
{
    [Fact]
    public void SupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var workspace = builder.AddAzureLogAnalyticsWorkspace("logAnalytics");
        
        // Verify NameOutputReference property is accessible
        var nameOutput = workspace.Resource.NameOutputReference;
        Assert.NotNull(nameOutput);
    }
}