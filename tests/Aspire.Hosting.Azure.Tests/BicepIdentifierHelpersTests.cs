// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class BicepIdentifierHelpersTests
{
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
