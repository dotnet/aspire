// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceViewModelNameComparerTests
{
    [Fact]
    public void Compare()
    {
        // Arrange
        var resources = new[]
        {
            ModelTestHelpers.CreateResource(resourceName: "database-dashboard-abc", displayName: "database-dashboard"),
            ModelTestHelpers.CreateResource(resourceName: "database-dashboard-xyz", displayName: "database-dashboard"),
            ModelTestHelpers.CreateResource(resourceName: "database-xyz", displayName: "database"),
            ModelTestHelpers.CreateResource(resourceName: "database-abc", displayName: "database"),
        };

        // Act
        var result = resources.OrderBy(v => v, ResourceViewModelNameComparer.Instance);

        // Assert
        Assert.Collection(result,
            vm => Assert.Equal("database-abc", vm.Name),
            vm => Assert.Equal("database-xyz", vm.Name),
            vm => Assert.Equal("database-dashboard-abc", vm.Name),
            vm => Assert.Equal("database-dashboard-xyz", vm.Name));
    }
}
