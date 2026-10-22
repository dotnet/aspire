// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire.Cli.DotNet;
using Aspire.Cli.Git;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Git;

public class GitRunnerTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloneAsync_UsesLiteralArgumentsAndHostAuthenticationPolicy(bool interactive)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var destination = new DirectoryInfo(Path.Combine(workspace.Path, "clone with spaces"));
        var processFactory = new TestProcessExecutionFactory();
        using var provider = CreateServices(workspace, processFactory, interactive).BuildServiceProvider();

        await provider.GetRequiredService<IGitRunner>().CloneAsync("-repository with spaces", destination, TestContext.Current.CancellationToken);

        Assert.Equal("git", processFactory.LastFileName);
        Assert.NotNull(processFactory.LastArguments);
        Assert.Equal(["clone", "--depth", "1", "--no-hardlinks", "--", "-repository with spaces", destination.FullName], processFactory.LastArguments);
        Assert.Equal(workspace.WorkspaceRoot.FullName, processFactory.LastWorkingDirectory?.FullName);
        if (interactive)
        {
            Assert.Null(processFactory.LastEnvironmentVariables);
        }
        else
        {
            var environment = Assert.Single(Assert.IsAssignableFrom<IDictionary<string, string>>(processFactory.LastEnvironmentVariables));
            Assert.Equal(new KeyValuePair<string, string>("GIT_TERMINAL_PROMPT", "0"), environment);
        }

        Assert.Equal(1, Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions)).DisposeCount);
    }

    [Fact]
    public async Task CloneAsync_NonzeroExitPreservesExitCodeAndDiagnostics()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var processFactory = new TestProcessExecutionFactory
        {
            AttemptCallback = (_, options) =>
            {
                options.StandardErrorCallback?.Invoke("Cloning into 'destination'...");
                options.StandardErrorCallback?.Invoke("fatal: repository not found");
                return (128, null);
            }
        };
        using var provider = CreateServices(workspace, processFactory, interactive: false).BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<GitCloneException>(() =>
            provider.GetRequiredService<IGitRunner>().CloneAsync("repository", workspace.WorkspaceRoot, TestContext.Current.CancellationToken));

        Assert.Equal(128, exception.ExitCode);
        Assert.Equal($"Cloning into 'destination'...{Environment.NewLine}fatal: repository not found", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(1, Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions)).DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloneAsync_MissingGitPreservesStartupFailure(bool failDuringStart)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var startupException = new Win32Exception(2);
        var processFactory = new TestProcessExecutionFactory
        {
            CreateExecutionWithFileNameCallback = (fileName, arguments, environment, _, options) =>
                failDuringStart
                    ? new TestProcessExecution(fileName, arguments, environment, options,
                        (_, _, _) => Task.FromResult((0, (string?)null)), () => 1) { StartException = startupException }
                    : throw startupException
        };
        using var provider = CreateServices(workspace, processFactory, interactive: false).BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<GitCloneException>(() =>
            provider.GetRequiredService<IGitRunner>().CloneAsync("repository", workspace.WorkspaceRoot, TestContext.Current.CancellationToken));

        Assert.Null(exception.ExitCode);
        Assert.Equal(startupException.Message, exception.Message);
        Assert.Same(startupException, exception.InnerException);
        if (failDuringStart)
        {
            Assert.Equal(1, Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions)).DisposeCount);
        }
        else
        {
            Assert.Empty(processFactory.CreatedExecutions);
        }
    }

    [Fact]
    public async Task CloneAsync_ProcessCannotStartThrowsAndDisposes()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var processFactory = new TestProcessExecutionFactory
        {
            CreateExecutionWithFileNameCallback = (fileName, arguments, environment, _, options) =>
                new TestProcessExecution(fileName, arguments, environment, options,
                    (_, _, _) => Task.FromResult((0, (string?)null)), () => 1) { StartReturnValue = false }
        };
        using var provider = CreateServices(workspace, processFactory, interactive: false).BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<GitCloneException>(() =>
            provider.GetRequiredService<IGitRunner>().CloneAsync("repository", workspace.WorkspaceRoot, TestContext.Current.CancellationToken));

        Assert.Null(exception.ExitCode);
        Assert.Null(exception.InnerException);
        var process = Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions));
        Assert.False(process.Started);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public async Task CloneAsync_AlreadyCanceledDoesNotCreateProcess()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var processFactory = new TestProcessExecutionFactory();
        using var provider = CreateServices(workspace, processFactory, interactive: false).BuildServiceProvider();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IGitRunner>().CloneAsync("repository", workspace.WorkspaceRoot, new CancellationToken(canceled: true)));

        Assert.Empty(processFactory.CreatedExecutions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloneAsync_CancellationDisposesProcess(bool processReportsSuccess)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var cancellation = new CancellationTokenSource();
        var processFactory = new TestProcessExecutionFactory
        {
            AsyncAttemptCallback = (_, _, token) =>
            {
                Assert.Equal(cancellation.Token, token);
                cancellation.Cancel();
                return processReportsSuccess
                    ? Task.FromResult((0, (string?)null))
                    : Task.FromCanceled<(int ExitCode, string? Stdout)>(token);
            }
        };
        using var provider = CreateServices(workspace, processFactory, interactive: false).BuildServiceProvider();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IGitRunner>().CloneAsync("repository", workspace.WorkspaceRoot, cancellation.Token));

        Assert.Equal(1, Assert.IsType<TestProcessExecution>(Assert.Single(processFactory.CreatedExecutions)).DisposeCount);
    }

    private IServiceCollection CreateServices(TemporaryWorkspace workspace, TestProcessExecutionFactory processFactory, bool interactive)
    {
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliHostEnvironmentFactory = _ => interactive
                ? TestHelpers.CreateInteractiveHostEnvironment()
                : TestHelpers.CreateNonInteractiveHostEnvironment();
        });
        services.AddSingleton<IProcessExecutionFactory>(processFactory);

        return services;
    }
}
