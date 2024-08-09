// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Dashboard.Tests;

public class GlobalizationTests
{
    [Fact]
    public void GetSupportedCultures_IncludesPopularCultures()
    {
        // Act
        var supportedCultures = DashboardWebApplication.GetSupportedCultures();

        // Assert
        foreach (var localizedCulture in DashboardWebApplication.LocalizedCultures)
        {
            Assert.Contains(localizedCulture, supportedCultures);
        }

        // A few cultures we expect to be available
        Assert.Contains("en-GB", supportedCultures);
        Assert.Contains("fr-CA", supportedCultures);
        Assert.Contains("zh-CN", supportedCultures);
    }
}
