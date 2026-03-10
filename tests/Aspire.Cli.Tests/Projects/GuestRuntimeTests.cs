// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Aspire.Hosting.Ats;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class GuestRuntimeTests
{
    private static RuntimeSpec CreateTestSpec(
        CommandSpec? execute = null,
        CommandSpec? watchExecute = null,
        CommandSpec? publishExecute = null,
        CommandSpec? installDependencies = null)
    {
        return new RuntimeSpec
        {
            Language = "test/runtime",
            DisplayName = "Test Runtime",
            CodeGenLanguage = "Test",
            DetectionPatterns = ["apphost.test"],
            Execute = execute ?? new CommandSpec
            {
                Command = "test-cmd",
                Args = ["{appHostFile}"]
            },
            WatchExecute = watchExecute,
            PublishExecute = publishExecute,
            InstallDependencies = installDependencies
        };
    }

    [Fact]
    public void Language_ReturnsSpecLanguage()
    {
        var runtime = new GuestRuntime(CreateTestSpec(), NullLogger.Instance);

        Assert.Equal("test/runtime", runtime.Language);
    }

    [Fact]
    public void DisplayName_ReturnsSpecDisplayName()
    {
        var runtime = new GuestRuntime(CreateTestSpec(), NullLogger.Instance);

        Assert.Equal("Test Runtime", runtime.DisplayName);
    }

    [Fact]
    public void CreateDefaultLauncher_ReturnsProcessGuestLauncher()
    {
        var runtime = new GuestRuntime(CreateTestSpec(), NullLogger.Instance);

        var launcher = runtime.CreateDefaultLauncher();

        Assert.IsType<ProcessGuestLauncher>(launcher);
    }

    [Fact]
    public async Task RunAsync_UsesExecuteSpec()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "my-runner",
            Args = ["{appHostFile}"]
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");
        var envVars = new Dictionary<string, string>();

        await runtime.RunAsync(appHostFile, directory, envVars, watchMode: false, launcher, CancellationToken.None);

        Assert.Equal("my-runner", launcher.LastCommand);
        Assert.Contains(appHostFile.FullName, launcher.LastArgs);
    }

    [Fact]
    public async Task RunAsync_WatchMode_UsesWatchExecuteSpec()
    {
        var spec = CreateTestSpec(
            execute: new CommandSpec { Command = "run-cmd", Args = ["{appHostFile}"] },
            watchExecute: new CommandSpec { Command = "watch-cmd", Args = ["--watch", "{appHostFile}"] }
        );
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.RunAsync(appHostFile, directory, new Dictionary<string, string>(), watchMode: true, launcher, CancellationToken.None);

        Assert.Equal("watch-cmd", launcher.LastCommand);
        Assert.Contains("--watch", launcher.LastArgs);
    }

    [Fact]
    public async Task RunAsync_WatchModeWithoutWatchSpec_FallsBackToExecute()
    {
        var spec = CreateTestSpec(execute: new CommandSpec { Command = "run-cmd", Args = ["{appHostFile}"] });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.RunAsync(appHostFile, directory, new Dictionary<string, string>(), watchMode: true, launcher, CancellationToken.None);

        Assert.Equal("run-cmd", launcher.LastCommand);
    }

    [Fact]
    public async Task PublishAsync_UsesPublishExecuteSpec()
    {
        var spec = CreateTestSpec(
            execute: new CommandSpec { Command = "run-cmd", Args = ["{appHostFile}"] },
            publishExecute: new CommandSpec { Command = "publish-cmd", Args = ["{appHostFile}", "{args}"] }
        );
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.PublishAsync(appHostFile, directory, new Dictionary<string, string>(), ["--output", "/out"], launcher, CancellationToken.None);

        Assert.Equal("publish-cmd", launcher.LastCommand);
        // {args} placeholder joins multiple args into a single string
        Assert.Contains(launcher.LastArgs, a => a.Contains("--output") && a.Contains("/out"));
    }

    [Fact]
    public async Task PublishAsync_WithoutPublishSpec_FallsBackToExecute()
    {
        var spec = CreateTestSpec(execute: new CommandSpec { Command = "run-cmd", Args = ["{appHostFile}"] });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.PublishAsync(appHostFile, directory, new Dictionary<string, string>(), null, launcher, CancellationToken.None);

        Assert.Equal("run-cmd", launcher.LastCommand);
    }

    [Fact]
    public async Task RunAsync_MergesSpecEnvironmentVariables()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "test-cmd",
            Args = ["{appHostFile}"],
            EnvironmentVariables = new Dictionary<string, string> { ["SPEC_VAR"] = "spec_value" }
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");
        var envVars = new Dictionary<string, string> { ["CALLER_VAR"] = "caller_value" };

        await runtime.RunAsync(appHostFile, directory, envVars, watchMode: false, launcher, CancellationToken.None);

        Assert.Equal("caller_value", launcher.LastEnvironmentVariables["CALLER_VAR"]);
        Assert.Equal("spec_value", launcher.LastEnvironmentVariables["SPEC_VAR"]);
    }

    [Fact]
    public async Task RunAsync_SpecEnvironmentVariables_TakePrecedence()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "test-cmd",
            Args = ["{appHostFile}"],
            EnvironmentVariables = new Dictionary<string, string> { ["SHARED_VAR"] = "from_spec" }
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");
        var envVars = new Dictionary<string, string> { ["SHARED_VAR"] = "from_caller" };

        await runtime.RunAsync(appHostFile, directory, envVars, watchMode: false, launcher, CancellationToken.None);

        Assert.Equal("from_spec", launcher.LastEnvironmentVariables["SHARED_VAR"]);
    }

    [Fact]
    public async Task RunAsync_ReplacesAppHostFilePlaceholder()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "npx",
            Args = ["tsx", "{appHostFile}"]
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/home/user/project/apphost.ts");
        var directory = new DirectoryInfo("/home/user/project");

        await runtime.RunAsync(appHostFile, directory, new Dictionary<string, string>(), watchMode: false, launcher, CancellationToken.None);

        Assert.Equal("npx", launcher.LastCommand);
        Assert.Equal(new[] { "tsx", appHostFile.FullName }, launcher.LastArgs);
    }

    [Fact]
    public async Task RunAsync_ReplacesAppHostDirPlaceholder()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "test-cmd",
            Args = ["--dir", "{appHostDir}"]
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/home/user/project/apphost.ts");
        var directory = new DirectoryInfo("/home/user/project");

        await runtime.RunAsync(appHostFile, directory, new Dictionary<string, string>(), watchMode: false, launcher, CancellationToken.None);

        Assert.Equal(new[] { "--dir", directory.FullName }, launcher.LastArgs);
    }

    [Fact]
    public async Task PublishAsync_AdditionalArgsAppendedWhenNoPlaceholder()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "test-cmd",
            Args = ["{appHostFile}"]
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.PublishAsync(appHostFile, directory, new Dictionary<string, string>(), ["--extra", "arg"], launcher, CancellationToken.None);

        Assert.Equal(appHostFile.FullName, launcher.LastArgs[0]);
        Assert.Equal("--extra", launcher.LastArgs[1]);
        Assert.Equal("arg", launcher.LastArgs[2]);
    }

    [Fact]
    public async Task RunAsync_EmptyPlaceholderReplacementsAreSkipped()
    {
        var spec = CreateTestSpec(execute: new CommandSpec
        {
            Command = "test-cmd",
            Args = ["{args}"]
        });
        var runtime = new GuestRuntime(spec, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        var appHostFile = new FileInfo("/tmp/apphost.ts");
        var directory = new DirectoryInfo("/tmp");

        await runtime.RunAsync(appHostFile, directory, new Dictionary<string, string>(), watchMode: false, launcher, CancellationToken.None);

        Assert.Empty(launcher.LastArgs);
    }

    [Fact]
    public async Task InstallDependenciesAsync_WithNoSpec_ReturnsZero()
    {
        var spec = CreateTestSpec();
        var runtime = new GuestRuntime(spec, NullLogger.Instance);

        var exitCode = await runtime.InstallDependenciesAsync(new DirectoryInfo("/tmp"), CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    private sealed class RecordingLauncher : IGuestProcessLauncher
    {
        public string LastCommand { get; private set; } = string.Empty;
        public string[] LastArgs { get; private set; } = [];
        public DirectoryInfo? LastWorkingDirectory { get; private set; }
        public IDictionary<string, string> LastEnvironmentVariables { get; private set; } = new Dictionary<string, string>();

        public Task<(int ExitCode, OutputCollector? Output)> LaunchAsync(
            string command,
            string[] args,
            DirectoryInfo workingDirectory,
            IDictionary<string, string> environmentVariables,
            CancellationToken cancellationToken)
        {
            LastCommand = command;
            LastArgs = args;
            LastWorkingDirectory = workingDirectory;
            LastEnvironmentVariables = new Dictionary<string, string>(environmentVariables);
            return Task.FromResult<(int, OutputCollector?)>((0, new OutputCollector()));
        }
    }
}
