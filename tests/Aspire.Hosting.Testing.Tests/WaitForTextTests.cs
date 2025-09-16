// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class WaitForTextTests
{
    [Fact]
    public async Task WaitForTextAsync_ThrowsArgumentNullException_WhenAppIsNull()
    {
        DistributedApplication app = null!;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => app.WaitForTextAsync("test"));
    }

    [Fact]
    public async Task WaitForTextAsync_ThrowsArgumentException_WhenLogTextIsEmpty()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        await using var app = await builder.BuildAsync();
        
        await Assert.ThrowsAsync<ArgumentException>(() => app.WaitForTextAsync(""));
    }

    [Fact]
    public async Task WaitForTextAsync_ThrowsArgumentNullException_WhenLogTextsIsNull()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        await using var app = await builder.BuildAsync();
        
        IEnumerable<string> logTexts = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() => app.WaitForTextAsync(logTexts));
    }

    [Fact]
    public async Task WaitForTextAsync_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        await using var app = await builder.BuildAsync();
        
        Predicate<string> predicate = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() => app.WaitForTextAsync(predicate, resourceName: null));
    }
}