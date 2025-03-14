// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.SqlServer.Tests;

public class SqlServerGoStatementTests
{
    [Theory]
    [InlineData("GO")]
    [InlineData(" GO")]
    [InlineData(" GO ")]
    [InlineData("  GO ")]
    [InlineData("  GO  ")]
    [InlineData("GO\n")]
    [InlineData("GO--")]
    [InlineData(" GO--")]
    [InlineData(" GO --")]
    [InlineData("GO -- comments")]
    [InlineData("GO 123")]
    [InlineData("GO 123 --")]
    [InlineData("GO 123 --comments")]
    public void DelimiterShouldMatch(string delimiter)
    {
        Assert.Matches(SqlServerBuilderExtensions.GoStatements(), delimiter);
    }

    [Theory]
    [InlineData("GO;")]
    [InlineData("GO-")]
    [InlineData("GO -comment")]
    [InlineData(";GO")]
    [InlineData("GO 1234567")]
    [InlineData("GO 123456 123")]
    [InlineData("a GO;")]
    [InlineData("-- GO")]
    [InlineData("GO a")]
    [InlineData("GO 123_123")]
    public void DelimiterShouldNotMatch(string delimiter)
    {
        Assert.DoesNotMatch(SqlServerBuilderExtensions.GoStatements(), delimiter);
    }
}
