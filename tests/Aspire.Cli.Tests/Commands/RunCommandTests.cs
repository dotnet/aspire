// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.DotNet;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.Commands;

public class RunCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task RunCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenNoProjectFileFound_ReturnsNonZeroExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new NoProjectFileProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenMultipleProjectFilesFound_ReturnsNonZeroExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new MultipleProjectFilesProjectLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenProjectFileDoesNotExist_ReturnsNonZeroExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new ProjectFileDoesNotExistLocator();
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run --project /tmp/doesnotexist.csproj");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    private sealed class ProjectFileDoesNotExistLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }
    }

    [Fact]
    public async Task RunCommand_WhenCertificateServiceThrows_ReturnsNonZeroExitCode()
    {
        var runnerFactory = (IServiceProvider sp) => {
            var runner = new TestDotNetCliRunner();

            // Fake apphost information to return a compatable app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            return runner;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CertificateServiceFactory = _ => new ThrowingCertificateService();
            options.DotNetCliRunnerFactory = runnerFactory;
            options.ProjectLocatorFactory = projectLocatorFactory;
        });

        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToTrustCertificates, exitCode);
    }

    private sealed class ThrowingCertificateService : Aspire.Cli.Certificates.ICertificateService
    {
        public Task EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Certificates.CertificateServiceException("Failed to trust certificates");
        }
    }

    private sealed class NoProjectFileProjectLocator : IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }
    }

    private sealed class MultipleProjectFilesProjectLocator : IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Multiple project files found.");
        }
    }
    private async IAsyncEnumerable<BackchannelLogEntry> ReturnLogEntriesUntilCancelledAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var logEntryIndex = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            // Simulate log entries being returned
            yield return new BackchannelLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                LogLevel = LogLevel.Information,
                Message = $"Test log entry {logEntryIndex++}",
                EventId = new EventId(),
                CategoryName = "TestCategory"
            };
        }
    }

    [Fact]
    public async Task RunCommand_CompletesSuccessfully()
    {
        var getResourceStatesAsyncCalled = new TaskCompletionSource();

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();

            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;

            return backchannel;
            
        };

        var runnerFactory = (IServiceProvider sp) => {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatable app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            // public Task<int> RunAsync(FileInfo projectFile, bool watch, bool noBuild, string[] args, IDictionary<string, string>? env, TaskCompletionSource<AppHostBackchannel>? backchannelCompletionSource, CancellationToken cancellationToken)
            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                // Make a backchannel and return it, but don't return from the run call until the backchannel 
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Just simulate the process running until the user cancels.
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);

                return 0;
            };

            return runner;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        var pendingRun = result.InvokeAsync(cts.Token);

        // Simulate CTRL-C.
        cts.Cancel();

        var exitCode = await pendingRun.WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task RunCommand_WithNoResources_CompletesSuccessfully()
    {
        var getResourceStatesAsyncCalled = new TaskCompletionSource();
        var backchannelFactory = (IServiceProvider sp) => {
            var backchannel = new TestAppHostBackchannel();

            // Return empty resources using an empty enumerable
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;

            return backchannel;
        };

        var runnerFactory = (IServiceProvider sp) => {
            var runner = new TestDotNetCliRunner();
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());
            
            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return 0;
            };

            return runner;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        var pendingRun = result.InvokeAsync(cts.Token);

        // Simulate CTRL-C.
        cts.Cancel();

        var exitCode = await pendingRun.WaitAsync(CliTestConstants.LongTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenDashboardFailsToStart_ReturnsNonZeroExitCodeWithClearErrorMessage()
    {
        var errorMessages = new List<string>();
        
        var backchannelFactory = (IServiceProvider sp) => {
            var backchannel = new TestAppHostBackchannel();
            // Configure the backchannel to throw ResourceFailedException when GetDashboardUrlsAsync is called
            backchannel.GetDashboardUrlsAsyncCallback = (ct) =>
            {
                throw new ResourceFailedException("aspire-dashboard", "FailedToStart", 
                    "Dashboard failed to start and entered a terminal state.");
            };
            return backchannel;
        };

        var runnerFactory = (IServiceProvider sp) => {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatible app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            // Configure the runner to establish a backchannel but simulate dashboard failure
            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                // Set up the backchannel
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Just simulate the process running until the user cancels.
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);

                return 0;
            };

            return runner;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
            options.InteractionServiceFactory = (sp) =>
            {
                var interactionService = new TestConsoleInteractionService();
                interactionService.DisplayErrorCallback = errorMessages.Add;
                return interactionService;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        
        // Assert that the command returns the expected failure exit code
        Assert.Equal(ExitCodeConstants.FailedToDotnetRunAppHost, exitCode);

        // Check that the error message contains the expected text
        Assert.Contains(errorMessages, msg => 
            msg.Contains("Dashboard failed to start") && 
            msg.Contains("aspire-dashboard") && 
            msg.Contains("FailedToStart"));
    }
}
