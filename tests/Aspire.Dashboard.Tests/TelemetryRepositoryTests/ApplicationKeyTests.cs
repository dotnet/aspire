// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Storage;
using Xunit;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class ApplicationKeyTests
{
    [Theory]
    [InlineData("name", "instanceid", "name-instanceid")]
    [InlineData("name", "instanceid", "NAME-INSTANCEID")]
    [InlineData("name", "752e1688-ca3c-45da-b48b-b2163296ac91", "name-752e1688-ca3c-45da-b48b-b2163296ac91")]
    public void EqualsCompositeName_Success(string name, string instanceId, string compositeName)
    {
        // Arrange
        var key = new ApplicationKey(name, instanceId);

        // Act
        var result = key.EqualsCompositeName(compositeName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("name", "instanceid", null)]
    [InlineData("name", "instanceid", "")]
    [InlineData("name", "instanceid", "name")]
    [InlineData("name", "instanceid", "instanceid")]
    [InlineData("name", "instanceid", "name_instanceid")]
    [InlineData("name", "instanceid", "instanceid-name")]
    public void EqualsCompositeName_Failure(string name, string instanceId, string compositeName)
    {
        // Arrange
        var key = new ApplicationKey(name, instanceId);

        // Act
        var result = key.EqualsCompositeName(compositeName);

        // Assert
        Assert.False(result);
    }
}
