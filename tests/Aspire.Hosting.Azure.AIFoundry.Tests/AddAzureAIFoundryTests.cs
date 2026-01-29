// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
// using Aspire.Hosting.Azure.AIFoundry;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class AddAzureAIFoundryTests
{
    [Fact]
    public void ShouldHaveDefaultConnectionStringEnvVar()
    {
        // Arrange
        const string name = "my-project";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureAIFoundry("account");
        var app = resourceBuilder.ApplicationBuilder.Build();

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<AzureCognitiveServicesProjectResource>(resourceBuilder.Resource);
    }
}
