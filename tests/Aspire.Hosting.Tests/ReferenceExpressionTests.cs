// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;
public class ReferenceExpressionTests
{
    [Theory]
    [InlineData("world", "Hello world", "Hello world")]
    [InlineData("{", "Hello {{", "Hello {")]
    [InlineData("}", "Hello }}", "Hello }")]
    [InlineData("{1}", "Hello {{1}}", "Hello {1}")]
    [InlineData("{x}", "Hello {{x}}", "Hello {x}")]
    [InlineData("{{x}}", "Hello {{{{x}}}}", "Hello {{x}}")]
    public void TestReferenceExpressionCreateInputStringTreatedAsLiteral(string input, string expectedFormat, string expectedExpression)
    {
        var refExpression = ReferenceExpression.Create($"Hello {input}");
        Assert.Equal(expectedFormat, refExpression.Format);

        // Generally, the input string should end up unchanged in the expression, since it's a literal
        var expr = refExpression.ValueExpression;
        Assert.Equal(expectedExpression, expr);
    }
}
