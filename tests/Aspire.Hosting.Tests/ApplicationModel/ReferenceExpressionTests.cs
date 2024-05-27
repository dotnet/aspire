// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.ApplicationModel;
public class ReferenceExpressionTests {
    [Fact]
    public void ReferenceExpressionHandlesValueWithNonParameterBrackets() {
        var s = "{x}";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("{x}", expr);
    }
    [Fact]
    public void ReferenceExpressionHandlesValueWithDoubleNonParameterBrackets() {
        var s = "{{x}}";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("{{x}}", expr);
    }
    [Fact]
    public void ReferenceExpressionHandlesValueWithUnmatchedNonParameterStartBracket() {
        var s = "{x";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("{x", expr);
    }
    [Fact]
    public void ReferenceExpressionHandlesValueWithUnmatchedNonParameterEndBracket() {
        var s = "x}";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("x}", expr);
    }
    [Fact]
    public void ReferenceExpressionHandlesValueWithParameterBrackets() {
        var s = "{0}";
        var expr = ReferenceExpression.Create($"{s}", [new HostUrl("test")], ["abc123"]).ValueExpression;
        Assert.Equal("abc123", expr);
    }
    [Fact]
    public void ReferenceExpressionHandlesValueWithoutBrackets() {
        var s = "Test";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.Equal("Test", expr);
    }
}
