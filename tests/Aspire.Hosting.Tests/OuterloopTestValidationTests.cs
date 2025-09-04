// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Hosting.Tests;

public class OuterloopTestValidationTests
{
    [Fact]
    public void RegularTest_ShouldRunInNormalCI()
    {
        // This test should run in regular CI
        Assert.True(true);
    }

    [Fact]
    [OuterloopTest("Test marked for outerloop execution only")]
    public void OuterloopTest_ShouldNotRunInNormalCI()
    {
        // This test should only run in outerloop CI
        Assert.True(true);
    }

    [Fact]
    [QuarantinedTest("Test marked as quarantined")]
    public void QuarantinedTest_ShouldNotRunInNormalCI()
    {
        // This test should only run in quarantine CI
        Assert.True(true);
    }
}