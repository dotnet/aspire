// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Caching;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.Configuration;
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
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }

        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task RunCommand_WhenCertificateServiceThrows_ReturnsNonZeroExitCode()
    {
        var runnerFactory = (IServiceProvider sp) =>
        {
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
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("No project file found.");
        }

        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MultipleProjectFilesProjectLocator : IProjectLocator
    {
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Multiple project files found.");
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Multiple project files found.");
        }

        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        var runnerFactory = (IServiceProvider sp) =>
        {
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
        var pendingRun = result.InvokeAsync(cancellationToken: cts.Token);

        // Simulate CTRL-C.
        cts.Cancel();

        var exitCode = await pendingRun.WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task RunCommand_WithNoResources_CompletesSuccessfully()
    {
        var getResourceStatesAsyncCalled = new TaskCompletionSource();
        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();

            // Return empty resources using an empty enumerable
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;

            return backchannel;
        };

        var runnerFactory = (IServiceProvider sp) =>
        {
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
        var pendingRun = result.InvokeAsync(cancellationToken: cts.Token);

        // Simulate CTRL-C.
        cts.Cancel();

        var exitCode = await pendingRun.WaitAsync(CliTestConstants.LongTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task RunCommand_WhenDashboardFailsToStart_ReturnsNonZeroExitCodeWithClearErrorMessage()
    {
        var errorMessages = new List<string>();

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();
            // Configure the backchannel to throw DashboardStartupException when GetDashboardUrlsAsync is called
            backchannel.GetDashboardUrlsAsyncCallback = (ct) =>
            {
                return Task.FromResult(new DashboardUrlsState
                {
                    DashboardHealthy = false,
                    BaseUrlWithLoginToken = null,
                    CodespacesUrlWithLoginToken = null
                });
            };
            return backchannel;
        };

        var runnerFactory = (IServiceProvider sp) =>
        {
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
        Assert.Equal(ExitCodeConstants.DashboardFailure, exitCode);
    }

    [Fact]
    public async Task AppHostHelper_BuildAppHostAsync_IncludesRelativePathInStatusMessage()
    {
        var testInteractionService = new TestConsoleInteractionService();
        testInteractionService.ShowStatusCallback = (statusText) =>
        {
            Assert.Contains(
                $"{InteractionServiceStrings.BuildingAppHost} src{Path.DirectorySeparatorChar}MyApp.AppHost{Path.DirectorySeparatorChar}MyApp.AppHost.csproj",
                statusText
            );
        };

        var testRunner = new TestDotNetCliRunner();
        testRunner.BuildAsyncCallback = (projectFile, options, ct) => 0;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostDirectoryPath = Path.Combine(workspace.WorkspaceRoot.FullName, "src", "MyApp.AppHost");
        var appHostDirectory = Directory.CreateDirectory(appHostDirectoryPath);
        var appHostProjectPath = Path.Combine(appHostDirectory.FullName, "MyApp.AppHost.csproj");
        var appHostProjectFile = new FileInfo(appHostProjectPath);
        File.WriteAllText(appHostProjectFile.FullName, "<Project></Project>");

        var options = new DotNetCliRunnerInvocationOptions();
        await AppHostHelper.BuildAppHostAsync(testRunner, testInteractionService, appHostProjectFile, options, workspace.WorkspaceRoot, CancellationToken.None);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11169")]
    public async Task RunCommand_SkipsBuild_WhenExtensionDevKitCapabilityIsAvailable()
    {
        var buildCalled = false;

        var extensionBackchannel = new TestExtensionBackchannel();
        extensionBackchannel.GetCapabilitiesAsyncCallback = ct => Task.FromResult(new[] { "devkit" });

        var appHostBackchannel = new TestAppHostBackchannel();
        appHostBackchannel.GetDashboardUrlsAsyncCallback = (ct) => Task.FromResult(new DashboardUrlsState
        {
            DashboardHealthy = true,
            BaseUrlWithLoginToken = "http://localhost/dashboard",
            CodespacesUrlWithLoginToken = null
        });
        appHostBackchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;

        var backchannelFactory = (IServiceProvider sp) => appHostBackchannel;

        var extensionInteractionServiceFactory = (IServiceProvider sp) => new TestExtensionInteractionService(sp);

        var runnerFactory = (IServiceProvider sp) => {
            var runner = new TestDotNetCliRunner();
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;
            runner.BuildAsyncCallback = (projectFile, options, ct) => {
                buildCalled = true;
                return 0;
            };
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());
            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) => {
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
            options.ExtensionBackchannelFactory = _ => extensionBackchannel;
            options.InteractionServiceFactory = extensionInteractionServiceFactory;
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        var pendingRun = result.InvokeAsync(cancellationToken: cts.Token);
        cts.Cancel();
        var exitCode = await pendingRun.WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.False(buildCalled, "Build should be skipped when extension DevKit capability is available.");
    }

    [Fact]
    public async Task RunCommand_WhenSingleFileAppHostAndDefaultWatchEnabled_DoesNotUseWatchMode()
    {
        var watchModeUsed = false;

        var runnerFactory = (IServiceProvider sp) =>
        {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatible app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                watchModeUsed = watch;
                // Make a backchannel and return it
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Don't run indefinitely for the test
                await Task.Delay(100, ct);
                return 0;
            };

            return runner;
        };

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;
            return backchannel;
        };

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new SingleFileAppHostProjectLocator();
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
            options.EnabledFeatures = [KnownFeatures.DefaultWatchEnabled];
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var exitCode = await result.InvokeAsync(cancellationToken: cts.Token);

        Assert.False(watchModeUsed, "Expected watch mode to be disabled for single file apps even when DefaultWatchEnabled feature flag is true");
    }

    [Fact]
    public async Task RunCommand_WhenDefaultWatchEnabledFeatureFlagIsTrue_UsesWatchMode()
    {
        var watchModeUsed = false;

        var runnerFactory = (IServiceProvider sp) =>
        {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatible app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                watchModeUsed = watch;
                // Make a backchannel and return it
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Don't run indefinitely for the test
                await Task.Delay(100, ct);
                return 0;
            };

            return runner;
        };

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;
            return backchannel;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
            options.EnabledFeatures = [KnownFeatures.DefaultWatchEnabled];
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var exitCode = await result.InvokeAsync(cancellationToken: cts.Token);

        Assert.True(watchModeUsed, "Expected watch mode to be enabled when defaultWatchEnabled feature flag is true");
    }

    [Fact]
    public async Task RunCommand_WhenDefaultWatchEnabledFeatureFlagIsFalse_DoesNotUseWatchMode()
    {
        var watchModeUsed = false;

        var runnerFactory = (IServiceProvider sp) =>
        {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatible app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                watchModeUsed = watch;
                // Make a backchannel and return it
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Don't run indefinitely for the test
                await Task.Delay(100, ct);
                return 0;
            };

            return runner;
        };

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;
            return backchannel;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
            options.DisabledFeatures = [KnownFeatures.DefaultWatchEnabled];
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var exitCode = await result.InvokeAsync(cancellationToken: cts.Token);

        Assert.False(watchModeUsed, "Expected watch mode to be disabled when defaultWatchEnabled feature flag is false");
    }

    [Fact]
    public async Task RunCommand_WhenDefaultWatchEnabledFeatureFlagNotSet_DefaultsToFalse()
    {
        var watchModeUsed = false;

        var runnerFactory = (IServiceProvider sp) =>
        {
            var runner = new TestDotNetCliRunner();

            // Fake the certificate check to always succeed
            runner.CheckHttpCertificateAsyncCallback = (options, ct) => 0;

            // Fake the build command to always succeed.
            runner.BuildAsyncCallback = (projectFile, options, ct) => 0;

            // Fake apphost information to return a compatible app host.
            runner.GetAppHostInformationAsyncCallback = (projectFile, options, ct) => (0, true, VersionHelper.GetDefaultTemplateVersion());

            runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, ct) =>
            {
                watchModeUsed = watch;
                // Make a backchannel and return it
                var backchannel = sp.GetRequiredService<IAppHostBackchannel>();
                backchannelCompletionSource!.SetResult(backchannel);

                // Don't run indefinitely for the test
                await Task.Delay(100, ct);
                return 0;
            };

            return runner;
        };

        var backchannelFactory = (IServiceProvider sp) =>
        {
            var backchannel = new TestAppHostBackchannel();
            backchannel.GetAppHostLogEntriesAsyncCallback = ReturnLogEntriesUntilCancelledAsync;
            return backchannel;
        };

        var projectLocatorFactory = (IServiceProvider sp) => new TestProjectLocator();

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = projectLocatorFactory;
            options.AppHostBackchannelFactory = backchannelFactory;
            options.DotNetCliRunnerFactory = runnerFactory;
            // Don't explicitly set the feature flag
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("run");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var exitCode = await result.InvokeAsync(cancellationToken: cts.Token);

        Assert.False(watchModeUsed, "Expected watch mode to be disabled by default when defaultWatchEnabled feature flag is not set");
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsTrue_IncludesNonInteractiveFlag()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that --non-interactive is included when watch mode is enabled
                Assert.Contains("watch", args);
                Assert.Contains("--non-interactive", args);

                // Verify the order: watch should come before --non-interactive
                var watchIndex = Array.IndexOf(args, "watch");
                var nonInteractiveIndex = Array.IndexOf(args, "--non-interactive");
                Assert.True(watchIndex < nonInteractiveIndex);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true, // This should add --non-interactive
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsFalse_DoesNotIncludeNonInteractiveFlag()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that --non-interactive is NOT included when watch mode is disabled
                Assert.Contains("run", args);
                Assert.DoesNotContain("watch", args);
                Assert.DoesNotContain("--non-interactive", args);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false, // This should NOT add --non-interactive
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsTrueAndDebugIsTrue_IncludesVerboseFlag()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions { Debug = true };

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that --verbose is included when watch mode and debug are both enabled
                Assert.Contains("watch", args);
                Assert.Contains("--verbose", args);

                // Verify the order: watch should come before --verbose
                var watchIndex = Array.IndexOf(args, "watch");
                var verboseIndex = Array.IndexOf(args, "--verbose");
                Assert.True(watchIndex < verboseIndex);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true, // This should add --verbose when debug is true
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsTrueAndDebugIsFalse_DoesNotIncludeVerboseFlag()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions { Debug = false };

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that --verbose is NOT included when debug is false
                Assert.Contains("watch", args);
                Assert.DoesNotContain("--verbose", args);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true, // This should NOT add --verbose when debug is false
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsFalseAndDebugIsTrue_DoesNotIncludeVerboseFlag()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions { Debug = true };

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that --verbose is NOT included when watch is false even if debug is true
                Assert.Contains("run", args);
                Assert.DoesNotContain("watch", args);
                Assert.DoesNotContain("--verbose", args);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false, // This should NOT add --verbose because it's not in watch mode
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsTrue_SetsSuppressLaunchBrowserEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER is set when watch mode is enabled
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"));
                Assert.Equal("true", env["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"]);
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true, // This should set DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=true
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DotNetCliRunner_RunAsync_WhenWatchIsFalse_DoesNotSetSuppressLaunchBrowserEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = new CliExecutionContext(
            workingDirectory: workspace.WorkspaceRoot, hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("hives"), cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire").CreateSubdirectory("cache"), sdksDirectory: new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-sdks"))
        );

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            executionContext,
            new NullDiskCache(),
            (args, env, workingDirectory, projectFile, backchannelCompletionSource, options) =>
            {
                // Verify that DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER is NOT set when watch mode is disabled
                if (env != null)
                {
                    Assert.False(env.ContainsKey("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"));
                }
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false, // This should NOT set DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    private sealed class SingleFileAppHostProjectLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AppHostProjectSearchResult(new FileInfo("/tmp/apphost.cs"), [new FileInfo("/tmp/apphost.cs")]));
        }

        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
        {
            // Return a .cs file to simulate single file AppHost
            return Task.FromResult<FileInfo?>(new FileInfo("/tmp/apphost.cs"));
        }

        public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FileInfo>>(new List<FileInfo>
            {
                new FileInfo(Path.Combine(searchDirectory, "AppHost1.csproj")),
                new FileInfo(Path.Combine(searchDirectory, "AppHost2.csproj"))
            });
        }
    }
}

internal sealed class AssertingDotNetCliRunner(
    ILogger<DotNetCliRunner> logger,
    IServiceProvider serviceProvider,
    AspireCliTelemetry telemetry,
    IConfiguration configuration,
    IFeatures features,
    IInteractionService interactionService,
    CliExecutionContext executionContext,
    IDiskCache diskCache,
    Action<string[], IDictionary<string, string>?, DirectoryInfo, FileInfo?, TaskCompletionSource<IAppHostBackchannel>?, DotNetCliRunnerInvocationOptions> assertionCallback,
    int exitCode
    ) : DotNetCliRunner(logger, serviceProvider, telemetry, configuration, features, interactionService, executionContext, diskCache)
{
    public override Task<int> ExecuteAsync(string[] args, IDictionary<string, string>? env, FileInfo? projectFile, DirectoryInfo workingDirectory, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        assertionCallback(args, env, workingDirectory, projectFile, backchannelCompletionSource, options);
        return Task.FromResult(exitCode);
    }
}
