// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("", "DefaultValue", "DefaultValue")]
    [InlineData("   ", "DefaultValue", "DefaultValue")]
    [InlineData("\t", "DefaultValue", "DefaultValue")]
    [InlineData("SingleNameOnly", null, "S")]
    [InlineData("singleNameOnly", null, "S")]
    [InlineData("Two Names", null, "TN")]
    [InlineData("two Names", null, "TN")]
    [InlineData("Two names", null, "TN")]
    [InlineData("two names", null, "TN")]
    [InlineData("With Three Names", null, "WN")]
    [InlineData("with Three Names", null, "WN")]
    [InlineData("With Three names", null, "WN")]
    [InlineData("with Three names", null, "WN")]
    [InlineData("With Hyphenated-Name", null, "WH")]
    [InlineData("with Hyphenated-Name", null, "WH")]
    [InlineData("With hyphenated-Name", null, "WH")]
    [InlineData("With Hyphenated-name", null, "WH")]
    [InlineData("with hyphenated-Name", null, "WH")]
    [InlineData("with Hyphenated-name", null, "WH")]
    [InlineData("with hyphenated-name", null, "WH")]
    public void GetInitials(string name, string? defaultValue, string expectedResult)
    {
        var actual = StringExtensions.GetInitials(name, defaultValue);

        Assert.Equal(expectedResult, actual);
    }
}
