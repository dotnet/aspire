// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Aspire.Hosting.ApplicationModel;
// using Aspire.Hosting.Utils;
using Azure.Provisioning;
// using Aspire.Hosting.Azure.AIFoundry;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class AppInsightsConnectionPropertiesTests
{
    [Fact]
    public void ShouldRenderToBicep()
    {
        // Arrange
        AppInsightsConnectionProperties props = new()
        {
            Target = new BicepValue<string>("my-insights-id"),
            IsSharedToAll = true
        };
        Assert.Equal(
            "{\r\n  isSharedToAll: true\r\n  target: 'my-insights-id'\r\n  authType: 'ApiKey'\r\n  category: 'AppInsights'\r\n}",
            props.ToBicepExpression().ToString()
        );
    }
}

public class AzureKeyVaultConnectionPropertiesTests
{
    [Fact]
    public void ShouldRenderToBicep()
    {
        // Arrange
        AzureKeyVaultConnectionProperties props = new()
        {
            Target = new BicepValue<string>("my-keyvault-id"),
            IsSharedToAll = true
        };
        Assert.Equal(
            "{\r\n  isSharedToAll: true\r\n  target: 'my-keyvault-id'\r\n  authType: 'ManagedIdentity'\r\n  category: 'AzureKeyVault'\r\n}",
            props.ToBicepExpression().ToString()
        );
    }
}
