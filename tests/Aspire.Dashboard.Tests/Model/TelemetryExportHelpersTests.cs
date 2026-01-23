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
        var result = TelemetryExportHelpers.GetResourceAsJson(resource, r => r.Name);

        // Assert
        Assert.Equal("test-resource.json", result.FileName);
        Assert.NotNull(result.Json);
    }

    [Fact]
    public void GetEnvironmentVariablesAsEnvFile_ReturnsExpectedFormat()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "test-resource",
            displayName: "Test Resource",
            resourceType: "Container",
            state: KnownResourceState.Running,
            environment: [
                new EnvironmentVariableViewModel("SIMPLE_VAR", "simple-value", fromSpec: false),
                new EnvironmentVariableViewModel("VAR_WITH_SPACES", "value with spaces", fromSpec: false),
                new EnvironmentVariableViewModel("EMPTY_VAR", "", fromSpec: false)
            ]);

        // Act
        var result = TelemetryExportHelpers.GetEnvironmentVariablesAsEnvFile(resource, r => r.Name);

        // Assert
        Assert.Equal("test-resource.env", result.FileName);
        Assert.NotNull(result.Content);
        
        // Verify the content contains expected entries
        Assert.Contains("EMPTY_VAR=", result.Content);
        Assert.Contains("SIMPLE_VAR=simple-value", result.Content);
        Assert.Contains("VAR_WITH_SPACES=\"value with spaces\"", result.Content);
    }

    [Fact]
    public void GetEnvironmentVariablesAsEnvFile_HandlesSpecialCharacters()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "test-resource",
            displayName: "Test Resource",
            resourceType: "Container",
            state: KnownResourceState.Running,
            environment: [
                new EnvironmentVariableViewModel("VAR_WITH_QUOTES", "value with \"quotes\"", fromSpec: false),
                new EnvironmentVariableViewModel("VAR_WITH_BACKSLASH", "path\\to\\file", fromSpec: false),
                new EnvironmentVariableViewModel("VAR_WITH_NEWLINE", "line1\nline2", fromSpec: false),
                new EnvironmentVariableViewModel("VAR_WITH_DOLLAR", "$HOME/path", fromSpec: false)
            ]);

        // Act
        var result = TelemetryExportHelpers.GetEnvironmentVariablesAsEnvFile(resource, r => r.Name);

        // Assert
        Assert.NotNull(result.Content);
        
        // Verify special characters are properly escaped/quoted
        Assert.Contains("VAR_WITH_QUOTES=\"value with \\\"quotes\\\"\"", result.Content);
        Assert.Contains("VAR_WITH_BACKSLASH=\"path\\\\to\\\\file\"", result.Content);
        Assert.Contains("VAR_WITH_NEWLINE=\"line1\\nline2\"", result.Content);
        Assert.Contains("VAR_WITH_DOLLAR=\"$HOME/path\"", result.Content);
    }

    [Fact]
    public void GetEnvironmentVariablesAsEnvFile_SortsVariablesAlphabetically()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "test-resource",
            displayName: "Test Resource",
            resourceType: "Container",
            state: KnownResourceState.Running,
            environment: [
                new EnvironmentVariableViewModel("ZEBRA", "last", fromSpec: false),
                new EnvironmentVariableViewModel("APPLE", "first", fromSpec: false),
                new EnvironmentVariableViewModel("MIDDLE", "middle", fromSpec: false)
            ]);

        // Act
        var result = TelemetryExportHelpers.GetEnvironmentVariablesAsEnvFile(resource, r => r.Name);

        // Assert
        var lines = result.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("APPLE=", lines[0]);
        Assert.StartsWith("MIDDLE=", lines[1]);
        Assert.StartsWith("ZEBRA=", lines[2]);
    }

    [Fact]
    public void GetEnvironmentVariablesAsEnvFile_HandlesNullValue()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "test-resource",
            displayName: "Test Resource",
            resourceType: "Container",
            state: KnownResourceState.Running,
            environment: [
                new EnvironmentVariableViewModel("NULL_VAR", null, fromSpec: false)
            ]);

        // Act
        var result = TelemetryExportHelpers.GetEnvironmentVariablesAsEnvFile(resource, r => r.Name);

        // Assert
        Assert.NotNull(result.Content);
        Assert.Contains("NULL_VAR=", result.Content);
    }
}
