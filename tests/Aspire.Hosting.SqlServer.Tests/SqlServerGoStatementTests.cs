// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Aspire.Hosting.SqlServer.Tests;
public class SqlServerGoStatementTests
{
    private static readonly Regex s_goStatements = (Regex)typeof(SqlServerBuilderExtensions).GetMethod("GoStatements", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null)!;

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
        Assert.Matches(s_goStatements, delimiter);
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
        Assert.DoesNotMatch(s_goStatements, delimiter);
    }
}
