// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class ExecCommandTests
{
    private readonly ITestOutputHelper _outputHelper;
    public ExecCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task ExecCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task ExecCommand_WhenNoProjectFileFound_ReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new NoProjectFileProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --resource api");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task ExecCommand_WhenMultipleProjectFilesFound_ReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new MultipleProjectFilesProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --resource api");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task ExecCommand_WhenProjectFileDoesNotExist_ReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new ProjectFileDoesNotExistLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --resource api");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task ExecCommand_WhenTargetResourceNotSpecified_ReturnsInvalidCommand()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --project test.csproj echo hello");

        // should not take long and fail fastly
        var exitCode = await result.InvokeAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task ExecCommand_ExecutesSuccessfully()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner
            {
                RunAsyncCallback = (projectFile, watch, noBuild, args, env, backchannelCompletionSource, runnerOptions, cancellationToken) =>
                {
                    var backchannel = new TestAppHostBackchannel();
                    backchannelCompletionSource?.SetResult(backchannel);
                    return Task.FromResult(0);
                }
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("exec --project test.csproj --resource myresource --command echo");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    private sealed class NoProjectFileProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }
    }

    private sealed class MultipleProjectFilesProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Multiple project files found.");
        }
    }

    private sealed class ProjectFileDoesNotExistLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }
    }
}
