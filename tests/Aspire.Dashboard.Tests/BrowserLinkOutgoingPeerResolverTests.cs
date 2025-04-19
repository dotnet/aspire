// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class BrowserLinkOutgoingPeerResolverTests
{
    [Fact]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [], out _));
    }

    [Fact]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "")], out _));
    }

    [Fact]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create<string, string>("http.url", null!)], out _));
    }

    // http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag

    [Fact]
    public void RelativeUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [Fact]
    public void NonLocalHostUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://dummy:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [Fact]
    public void NoPathGuidUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/getScriptTag")], out _));
    }

    [Fact]
    public void InvalidUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "ht$tp://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [Fact]
    public void NoPathUrlAttribute_Match()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/")], out _));
    }

    [Fact]
    public void GuidPathUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.False(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/not-a-guid/getScriptTag")], out _));
    }

    [Fact]
    public void LocalHostAndPathUrlAttribute_Match()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.True(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out var name));
        Assert.Equal("Browser Link", name);
    }

    private static bool TryResolvePeerName(IOutgoingPeerResolver resolver, KeyValuePair<string, string>[] attributes, out string? peerName)
    {
        return resolver.TryResolvePeerName(attributes, out peerName, out _);
    }
}
