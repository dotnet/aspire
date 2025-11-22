// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureAIFoundryConnectionPropertiesTests
{
    [Fact]
    public void AzureAIFoundryResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var aiFoundry = builder.AddAzureAIFoundry("aifoundry");

        var properties = ((IResourceWithConnectionString)aiFoundry.Resource).GetConnectionProperties().ToArray();

        Assert.Equal(2, properties.Length);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{aifoundry.outputs.endpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Key", property.Key);
                // Key is empty unless ApiKey is set or resource is in emulator mode
                Assert.Equal("", property.Value.ValueExpression);
            });
    }
}
