// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Agents;

public class NativePluginRegistrationTests(ITestOutputHelper output) : IDisposable
{
    private readonly AgentConfigurationTestContext _context = new(output);

    [Fact]
    public async Task SharedSettings_ArePublishedOnceAndReportBothClients()
    {
        _context.SetVariable("COPILOT_HOME", _context.ClaudeDirectory);

        var results = await _context.ConfigureIntegrationAsync(
            AgentConfigurationScope.Global, [_context.Copilot, _context.ClaudeCode]);

        var result = Assert.Single(results);
        Assert.Equal(AgentConfigurationStatus.Configured, result.Status);
        Assert.Equal([_context.Copilot, _context.ClaudeCode], result.Clients);
        Assert.Equal([result.TargetPath], Directory.EnumerateFiles(_context.Home.FullName, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    public async Task SharedSettings_MustSatisfyEverySelectedClientsPolicy(string suffix)
    {
        _context.SetVariable("COPILOT_HOME", Path.Combine(_context.ClaudeDirectory, suffix));
        var path = Path.Combine(_context.ClaudeDirectory, "settings.json");
        const string original = """{"preserved":true}""";
        await AgentConfigurationTestContext.WriteAsync(path, original);
        await AgentConfigurationTestContext.WriteAsync(
            Path.Combine(_context.ClaudeManagedDirectory, "managed-settings.json"),
            """{"enabledPlugins":{"aspire@aspire-skills":false}}""");

        var results = await _context.ConfigureIntegrationAsync(
            AgentConfigurationScope.Global, [_context.Copilot, _context.ClaudeCode]);

        var result = Assert.Single(results);
        Assert.Equal(AgentConfigurationStatus.Failed, result.Status);
        Assert.Equal([_context.Copilot, _context.ClaudeCode], result.Clients);
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task OneUnreadableFile_DoesNotStopIndependentClients()
    {
        var unreadable = Path.Combine(_context.Project.FullName, ".github", "copilot", "settings.json");
        Directory.CreateDirectory(unreadable);
        var error = await Record.ExceptionAsync(() => File.ReadAllBytesAsync(unreadable));
        Assert.NotNull(error);

        var results = await _context.ConfigureIntegrationAsync(
            AgentConfigurationScope.Project, [_context.Copilot, _context.ClaudeCode]);

        Assert.Equal([AgentConfigurationStatus.Failed, AgentConfigurationStatus.Configured], results.Select(result => result.Status));
        Assert.Equal(error.Message, results[0].Message);
        Assert.True(File.Exists(Path.Combine(_context.Project.FullName, ".claude", "settings.json")));
    }

    [Fact]
    public async Task Cancellation_PropagatesWithoutCreatingConfiguration()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _context.ConfigureIntegrationAsync(
            AgentConfigurationScope.Project, [_context.Copilot], cancellation.Token)).DefaultTimeout();

        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
    }

    public void Dispose() => _context.Dispose();
}
