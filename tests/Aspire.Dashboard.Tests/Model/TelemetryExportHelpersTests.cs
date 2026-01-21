// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class TelemetryExportHelpersTests
{
    [Fact]
    public void GetResourceAsJson_ReturnsExpectedJson()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "test-resource",
            displayName: "Test Resource",
            resourceType: "Container",
            state: KnownResourceState.Running,
            urls: [new UrlViewModel("http", new Uri("http://localhost:5000"), isInternal: false, isInactive: false, UrlDisplayPropertiesViewModel.Empty)],
            environment: [new EnvironmentVariableViewModel("MY_VAR", "my-value", fromSpec: false)],
            relationships: [new RelationshipViewModel("dependency", "Reference")]);

        // Act
        var result = TelemetryExportHelpers.GetResourceAsJson(resource);

        // Assert
        Assert.Equal("test-resource.json", result.FileName);
        Assert.NotNull(result.Json);
    }
}
