// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ModelNameTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abc123")]
    [InlineData("abc-123")]
    [InlineData("a-b-c-1-2-3")]
    [InlineData("ABC")]
    [InlineData("a_b")]
    [InlineData("a.b")]
    public void ValidateName_ValidNames_Success(string name)
    {
        ModelName.ValidateName(nameof(Resource), name);
    }
}
