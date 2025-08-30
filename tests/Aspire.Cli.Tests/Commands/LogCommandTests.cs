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
    public async Task LogCommand_WhenResourceNameNotProvided_ReturnsInvalidArguments()
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
    public async Task LogCommand_WhenNegativeLineCount_ReturnsInvalidArguments()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log test-resource --lines -1");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task LogCommand_WhenZeroLineCount_ReturnsInvalidArguments()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log test-resource --lines 0");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task LogCommand_WhenNoProjectFileFound_ReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new NoProjectFileProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log test-resource");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public void LogCommand_WhenValidArguments_ParsesCorrectly()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("log test-resource --lines 50 --tail --project test.csproj");

        Assert.NotNull(result);
        Assert.Empty(result.Errors);

        // Test that arguments are parsed correctly
        Assert.Equal("test-resource", result.GetValue<string>("resource-name"));
        Assert.Equal(50, result.GetValue<int>("--lines"));
        Assert.True(result.GetValue<bool>("--tail"));
        Assert.NotNull(result.GetValue<FileInfo?>("--project"));
    }

    private sealed class NoProjectFileProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }

        public Task<List<FileInfo>> FindAppHostProjectFilesAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }
    }
}