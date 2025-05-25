// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class ObsoleteResourceTests
{
    [Fact]
    public void AzureApplicationInsightsSupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        
        var appInsights = builder.AddAzureApplicationInsights("appInsights");
        
        // Verify NameOutputReference property is accessible
        var nameOutputReference = appInsights.Resource.NameOutputReference;
        Assert.NotNull(nameOutputReference);
    }
    
    [Fact]
    public void AzureLogAnalyticsWorkspaceSupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        
        var workspace = builder.AddAzureLogAnalyticsWorkspace("logAnalytics");
        
        // Verify NameOutputReference property is accessible
        var nameOutputReference = workspace.Resource.NameOutputReference;
        Assert.NotNull(nameOutputReference);
    }

    [Fact]
    public void AzureUserAssignedIdentitySupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        
        var identity = builder.AddAzureUserAssignedIdentity("identity");
        
        // Verify NameOutputReference property is accessible
        var nameOutputReference = identity.Resource.NameOutputReference;
        Assert.NotNull(nameOutputReference);
    }
}