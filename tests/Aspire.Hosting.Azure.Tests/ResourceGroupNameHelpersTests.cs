// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class ResourceGroupNameHelpersTests
{
    [Theory]
    [InlineData("äæǽåàçéïôùÀÇÉÏÔÙ", "aaaceiouACEIOU")]
    [InlineData("🔥🤔😅🤘", "")]
    [InlineData("こんにちは", "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("-.()_", "-_")]
    [InlineData("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_")]
    public void ShouldCreateAzdCompatibleResourceGroupNames(string input, string expected)
    {
        var result = ResourceGroupNameHelpers.NormalizeResourceGroupName(input);

        Assert.Equal(expected, result);
    }
}
