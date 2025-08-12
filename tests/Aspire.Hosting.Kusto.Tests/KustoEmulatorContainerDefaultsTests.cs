// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kusto.Tests;

/// <summary>
/// Tests for <see cref="KustoEmulatorContainerDefaults"/>.
/// </summary>
public class KustoEmulatorContainerDefaultsTests
{
    [Fact]
    public void DefaultTargetPort_ShouldBe8080()
    {
        // Assert
        Assert.Equal(8080, KustoEmulatorContainerDefaults.DefaultTargetPort);
    }

    [Fact]
    public void DefaultDbName_ShouldBeNetDefaultDB()
    {
        // Assert
        Assert.Equal("NetDefaultDB", KustoEmulatorContainerDefaults.DefaultDbName);
    }
}
