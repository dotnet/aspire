// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class AzureKustoConnectionPropertiesTests
{
    [Fact]
    public void AzureKustoClusterResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var kusto = builder.AddAzureKustoCluster("kusto");

        var properties = ((IResourceWithConnectionString)kusto.Resource).GetConnectionProperties().ToArray();

        Assert.Single(properties);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{kusto.outputs.clusterUri}", property.Value.ValueExpression);
            });
    }
}
