// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class AddAzureAIFoundryTests
{
    [Fact]
    public void AddAzureAIFoundry_ShouldAddResourceToBuilder()
    {
        const string name = "account";
        using var builder = TestDistributedApplicationBuilder.Create();

        var resourceBuilder = builder.AddAzureAIFoundry(name);

        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<AzureAIFoundryResource>(resourceBuilder.Resource);
    }
}
