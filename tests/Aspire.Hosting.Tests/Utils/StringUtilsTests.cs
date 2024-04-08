// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class StringUtilsTests
{
    [Theory]
    [InlineData("äæǽåàçéïôùÀÇÉÏÔÙ", "äæǽåàçéïôùÀÇÉÏÔÙ")]
    [InlineData("🔥🤔😅🤘", "")]
    [InlineData("こんにちは", "こんにちは")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("-.()_", "-.()_")]
    [InlineData("abc.", "abc")]
    [InlineData("abc...", "abc")]
    public void ShouldNormalizeResourceGroupNames(string input, string expected)
    {
        var result = ResourceGroupNameHelpers.NormalizeResourceGroupName(input);

        Assert.Equal(expected, result);
    }
}
