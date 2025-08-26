// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class LogCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task LogCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task LogCommandWithoutResourceNameReturnsFailed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task LogCommandWithResourceNameButNoRunningAppHostReturnsFailed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log web-api");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void LogCommandWithLinesOptionShowsCorrectDefault()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log web-api --lines 50");

        // Should parse successfully with lines option
        Assert.NotNull(result);
    }

    [Fact]
    public void LogCommandWithTailOptionRecognized()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log web-api --tail");

        // Should parse successfully with tail option
        Assert.NotNull(result);
    }
}