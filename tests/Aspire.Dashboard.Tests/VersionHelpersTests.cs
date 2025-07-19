// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class VersionHelpersTests
{
    // Accepts versions like 8.0.3, 9.0.0-preview.6.24224.1
    private static readonly Regex s_versionRegex = new Regex(@"^\d+\.\d+\.\d+(-[A-Za-z0-9\.\-]+)?$");

    [Fact]
    public void RuntimeVersion_ReturnsValidVersionString()
    {
        var version = VersionHelpers.RuntimeVersion;

        Assert.False(string.IsNullOrWhiteSpace(version), "Version should not be null or empty");
        Assert.Matches(s_versionRegex, version);
    }
}
