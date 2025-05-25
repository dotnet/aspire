// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Utils;
using Xunit;

public class NameOutputReferenceTests
{
    [Fact]
    public void AzureApplicationInsightsSupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var appInsights = builder.AddAzureApplicationInsights("appInsights");
        
        Assert.NotNull(appInsights.Resource.NameOutputReference);
    }
    
    [Fact]
    public void AzureLogAnalyticsWorkspaceSupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var workspace = builder.AddAzureLogAnalyticsWorkspace("workspace");
        
        Assert.NotNull(workspace.Resource.NameOutputReference);
    }
    
    [Fact]
    public void AzureUserAssignedIdentitySupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var identity = builder.AddAzureUserAssignedIdentity("identity");
        
        Assert.NotNull(identity.Resource.NameOutputReference);
    }
}