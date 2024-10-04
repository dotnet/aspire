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
    [InlineData("my___variable_", "my_\u212A_variable-")] // tests the Kelvin sign
    [InlineData("_my_variable_", "\u0130my_variable-")] // non-ASCII letter
    public void TestNormalize(string expected, string value)
    {
        Assert.Equal(expected, BicepIdentifierHelpers.Normalize(value));
    }

    [Theory]
    [InlineData("my-variable")]
    [InlineData("my variable")]
    [InlineData("_my-variable")]
    [InlineData("_my variable")]
    [InlineData("1my_variable")]
    [InlineData("1my-variable")]
    [InlineData("1my variable")]
    [InlineData("my_variable@")]
    [InlineData("my_variable-")]
    [InlineData("my_\u212A_variable")] // tests the Kelvin sign
    [InlineData("my_\u0130_variable")] // non-ASCII letter
    public void TestThrowIfInvalid(string value)
    {
        var e = Assert.Throws<ArgumentException>(() => BicepIdentifierHelpers.ThrowIfInvalid(value));

        // Verify the parameter name is from the caller member name. In this case, the "value" parameter above
        Assert.Equal(nameof(value), e.ParamName); 
    }
}
