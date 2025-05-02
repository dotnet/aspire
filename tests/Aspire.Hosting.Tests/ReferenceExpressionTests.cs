// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
    [InlineData("{{x}}", "Hello {{x}}", "Hello {x}")]
    public void TestReferenceExpressionCreateInputStringTreatedAsLiteral(string input, string expectedFormat, string expectedExpression)
    {
        var refExpression = ReferenceExpression.Create($"Hello {input}");
        Assert.Equal(expectedFormat, refExpression.Format);

        // Generally, the input string should end up unchanged in the expression, since it's a literal
        var expr = refExpression.ValueExpression;
        Assert.Equal(expectedExpression, expr);
    }

    [Theory]
    [InlineData("{x}", "{x}")]
    [InlineData("{x", "{x")]
    [InlineData("x}", "x}")]
    [InlineData("{1 var}", "{1 var}")]
    [InlineData("{var 1}", "{var 1}")]
    [InlineData("{1myVar}", "{1myVar}")]
    [InlineData("{myVar1}", "{myVar1}")]
    public void ReferenceExpressionHandlesValueWithNonParameterBrackets(string input, string expected)
    {
        var expr = ReferenceExpression.Create($"{input}").ValueExpression;
        Assert.Equal(expected, expr);
    }

    [Theory]
    [InlineData("{0}", new string[] { "abc123" }, "abc123")]
    [InlineData("{0} test", new string[] { "abc123" }, "abc123 test")]
    [InlineData("test {0}", new string[] { "abc123" }, "test abc123")]
    [InlineData("https://{0}:{1}/{2}?key={3}", new string[] { "test.com", "443", "path", "1234" }, "https://test.com:443/path?key=1234")]
    public void ReferenceExpressionHandlesValueWithParameterBrackets(string input, string[] parameters, string expected)
    {
        var expr = ReferenceExpression.Create($"{input}", [new HostUrl("test")], parameters).ValueExpression;
        Assert.Equal(expected, expr);
    }

    public static readonly object[][] ValidFormattingInParameterBracketCases = [
        ["{0:D}", new DateTime(2024,05,22)],
        ["{0:N}", 123456.78]
    ];

    [Theory, MemberData(nameof(ValidFormattingInParameterBracketCases))]
    public void ReferenceExpressionHandlesValueWithFormattingInParameterBrackets(string input, string parameterValue)
    {
        var expected = string.Format(CultureInfo.InvariantCulture, input, parameterValue);

        var expr = ReferenceExpression.Create($"{input}", [new HostUrl("test")], [parameterValue]).ValueExpression;
        Assert.Equal(expected, expr);
    }

    [Fact]
    public void ReferenceExpressionHandlesValueWithoutBrackets()
    {
        var s = "Test";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("Test", expr);
    }

    [Fact]
    public async Task ReferenceExpressionWithBracketsAndInterpolation()
    {
        var v = new Value();

        var expr = ReferenceExpression.Create($"[{{\"api_uri\":\"{v}\"}}]");

        Assert.Equal("[{\"api_uri\":\"{value}\"}]", expr.ValueExpression);
        Assert.Equal("[{\"api_uri\":\"Hello World\"}]", await expr.GetValueAsync(default));
    }

    [Fact]
    public async Task ReferenceExpressionWithEscapedBracketsAndInterpolation()
    {
        var v = new Value();

        var expr = ReferenceExpression.Create($"[{{{{\"api_uri\":\"{v}\"}}}}]");

        Assert.Equal("[{\"api_uri\":\"{value}\"}]", expr.ValueExpression);
        Assert.Equal("[{\"api_uri\":\"Hello World\"}]", await expr.GetValueAsync(default));
    }

    private sealed class Value : IValueProvider, IManifestExpressionProvider
    {
        public string ValueExpression => "{value}";

        public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
        {
            return new("Hello World");
        }
    }
}
