// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class DeferredValueProviderTests
{
    [Fact]
    public async Task GetValueAsync_ReturnsCallbackResult()
    {
        var provider = new DeferredValueProvider(
            () => "hello",
            () => "manifest-expression");

        var value = await provider.GetValueAsync();
        Assert.Equal("hello", value);
    }

    [Fact]
    public async Task GetValueAsync_ReturnsNullWhenCallbackReturnsNull()
    {
        var provider = new DeferredValueProvider(
            () => null,
            () => "");

        var value = await provider.GetValueAsync();
        Assert.Null(value);
    }

    [Fact]
    public void ValueExpression_ReturnsManifestCallbackResult()
    {
        var provider = new DeferredValueProvider(
            () => "runtime-value",
            () => "manifest-expression");

        Assert.Equal("manifest-expression", provider.ValueExpression);
    }

    [Fact]
    public async Task GetValueAsync_ResolvesLazilyFromCurrentState()
    {
        var enabled = false;
        var provider = new DeferredValueProvider(
            () => enabled ? ",ssl=true" : "");

        // Before enabling
        var valueBefore = await provider.GetValueAsync();
        Assert.Equal("", valueBefore);
        Assert.Equal("", provider.ValueExpression);

        // After enabling
        enabled = true;
        var valueAfter = await provider.GetValueAsync();
        Assert.Equal(",ssl=true", valueAfter);
        Assert.Equal(",ssl=true", provider.ValueExpression);
    }

    [Fact]
    public async Task GetValueAsync_WithContext_ReceivesValueProviderContext()
    {
        var context = new ValueProviderContext
        {
            Caller = null,
            Network = null
        };

        ValueProviderContext? capturedContext = null;
        var provider = new DeferredValueProvider(
            (ctx) =>
            {
                capturedContext = ctx;
                return "context-aware-value";
            });

        var value = await provider.GetValueAsync(context);
        Assert.Equal("context-aware-value", value);
        Assert.Same(context, capturedContext);
    }

    [Fact]
    public void ValueExpression_WithContextCallback_InvokesWithEmptyContext()
    {
        var provider = new DeferredValueProvider(
            (ValueProviderContext _) => "context-value");

        Assert.Equal("context-value", provider.ValueExpression);
    }

    [Fact]
    public async Task GetValueAsync_WithContextAndManifestCallback_UsesIndependentCallbacks()
    {
        var provider = new DeferredValueProvider(
            (ValueProviderContext _) => "runtime-value",
            () => "manifest-expression");

        var value = await provider.GetValueAsync();
        Assert.Equal("runtime-value", value);
        Assert.Equal("manifest-expression", provider.ValueExpression);
    }

    [Fact]
    public async Task SingleCallback_NullReturnsTreatedAsEmptyForManifest()
    {
        var provider = new DeferredValueProvider(() => (string?)null);

        var value = await provider.GetValueAsync();
        Assert.Null(value);
        Assert.Equal("", provider.ValueExpression);
    }

    [Fact]
    public async Task DeferredValueProvider_WorksInReferenceExpressionBuilder()
    {
        var enabled = false;
        var tlsFragment = new DeferredValueProvider(
            () => enabled ? ",ssl=true" : "");

        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral("localhost:6379");
        builder.Append($"{tlsFragment}");
        var expression = builder.Build();

        // Before enabling, runtime value does not include TLS
        var valueBefore = await expression.GetValueAsync(new(), default);
        Assert.Equal("localhost:6379", valueBefore);

        // Manifest expression also does not include TLS (captured at build time)
        Assert.Equal("localhost:6379", expression.ValueExpression);

        // After enabling, runtime value includes TLS dynamically
        enabled = true;
        var valueAfter = await expression.GetValueAsync(new(), default);
        Assert.Equal("localhost:6379,ssl=true", valueAfter);

        // But the manifest expression was captured at build time (when enabled was false),
        // so it still shows the old value
        Assert.Equal("localhost:6379", expression.ValueExpression);

        // A newly built expression captures the updated manifest expression
        var builder2 = new ReferenceExpressionBuilder();
        builder2.AppendLiteral("localhost:6379");
        builder2.Append($"{tlsFragment}");
        var expression2 = builder2.Build();
        Assert.Equal("localhost:6379,ssl=true", expression2.ValueExpression);
    }
}
