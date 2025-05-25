// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class ApplicationInsightsTests
{
    [Fact]
    public void SupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var appInsights = builder.AddAzureApplicationInsights("appInsights");
        
        // Verify NameOutputReference property is accessible
        var nameOutput = appInsights.Resource.NameOutputReference;
        Assert.NotNull(nameOutput);
    }
}