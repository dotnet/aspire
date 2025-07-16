// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class VersionHelpersTests
{
    [Fact]
    public void RuntimeVersion_ReturnsValue()
    {
        Assert.NotNull(VersionHelpers.RuntimeVersion);
    }
}
