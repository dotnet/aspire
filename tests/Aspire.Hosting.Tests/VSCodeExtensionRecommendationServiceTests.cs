// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class VSCodeExtensionRecommendationServiceTests
{
    [Fact]
    public void VSCodeServiceRegisteredWhenEnvironmentVariableIsSet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Set the environment variable that should trigger service registration
        builder.Configuration["ASPIRE_VSCODE_EXTENSION_RECOMMENDATIONS"] = "true";

        using var app = builder.Build();

        // Check if the service was registered
        var serviceDescriptors = builder.Services.Where(s => s.ServiceType.Name.Contains("VSCodeExtensionRecommendationService")).ToList();

        Assert.True(serviceDescriptors.Count > 0, "VSCode Extension Recommendation Service should be registered when ASPIRE_VSCODE_EXTENSION_RECOMMENDATIONS is true");
    }

    [Fact] 
    public void VSCodeServiceNotRegisteredWhenEnvironmentVariableIsNotSet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Don't set the environment variable
        using var app = builder.Build();

        // Check if the service was NOT registered
        var serviceDescriptors = builder.Services.Where(s => s.ServiceType.Name.Contains("VSCodeExtensionRecommendationService")).ToList();

        Assert.True(serviceDescriptors.Count == 0, "VSCode Extension Recommendation Service should NOT be registered when ASPIRE_VSCODE_EXTENSION_RECOMMENDATIONS is not set");
    }

    [Fact]
    public void VSCodeServiceNotRegisteredWhenEnvironmentVariableIsFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Set the environment variable to false
        builder.Configuration["ASPIRE_VSCODE_EXTENSION_RECOMMENDATIONS"] = "false";

        using var app = builder.Build();

        // Check if the service was NOT registered
        var serviceDescriptors = builder.Services.Where(s => s.ServiceType.Name.Contains("VSCodeExtensionRecommendationService")).ToList();

        Assert.True(serviceDescriptors.Count == 0, "VSCode Extension Recommendation Service should NOT be registered when ASPIRE_VSCODE_EXTENSION_RECOMMENDATIONS is false");
    }
}