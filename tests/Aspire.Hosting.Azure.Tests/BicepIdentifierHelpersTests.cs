// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class BicepIdentifierHelpersTests
{
    [Theory]
    [InlineData("my_variable", "my_variable")]
    [InlineData("my_variable", "my-variable")]
    [InlineData("my_variable", "my variable")]
    [InlineData("_my_variable", "_my_variable")]
    [InlineData("_my_variable", "_my-variable")]
    [InlineData("_my_variable", "_my variable")]
    [InlineData("_1my_variable", "1my_variable")]
    [InlineData("_1my_variable", "1my-variable")]
    [InlineData("_1my_variable", "1my variable")]
    [InlineData("my_variable9", "my_variable9")]
    [InlineData("my_variable_", "my_variable@")]
    [InlineData("my_variable_", "my_variable-")]
    public void TestNormalize(string expected, string value)
    {
        Assert.Equal(expected, BicepIdentifierHelpers.Normalize(value));
    }
}
