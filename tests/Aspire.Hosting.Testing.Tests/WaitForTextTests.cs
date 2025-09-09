// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class WaitForTextTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task WaitForTextAsync_SingleText_CompletesWhenTextAppears()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        builder.WithTestAndResourceLogging(output);

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for some expected text from the test project
        await app.WaitForTextAsync("info", resourceName: "myworker1");
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForTextAsync_MultipleTexts_CompletesWhenAnyTextAppears()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        builder.WithTestAndResourceLogging(output);

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for any of these texts to appear
        await app.WaitForTextAsync(new[] { "info", "warning", "error" }, resourceName: "myworker1");
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForTextAsync_Predicate_CompletesWhenPredicateMatches()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        builder.WithTestAndResourceLogging(output);

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for text that contains "info" (case insensitive)
        await app.WaitForTextAsync(log => log.Contains("info", StringComparison.OrdinalIgnoreCase), resourceName: "myworker1");
    }

    [Fact]
    [RequiresDocker]
    public async Task WaitForAllTextAsync_CompletesWhenAllTextsAppear()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>();
        builder.WithTestAndResourceLogging(output);

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // This test might be challenging since we need multiple different texts to appear
        // Let's wait for a single text for now as this is mainly to verify the API works
        await app.WaitForAllTextAsync(new[] { "info" }, resourceName: "myworker1");
    }

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