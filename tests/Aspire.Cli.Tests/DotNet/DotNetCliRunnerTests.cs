// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Cli.Tests.DotNet;

public class DotNetCliRunnerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DotNetCliCorrectlyAppliesNoLaunchProfileArgumentWhenSpecifiedInOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = true
        };

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, _, _, _, _) => Assert.Contains(args, arg => arg == "--no-launch-profile"),
            42
            );

        // This is what we are really testing here - that RunAsync reads
        // the NoLaunchProfile property from the invocation options and
        // correctly applies it to the command-line arguments for the
        // dotnet run command.
        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false,
            noBuild: false,
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
            );

        Assert.Equal(42, exitCode);
    }

    [Fact]
    public async Task BuildAsyncAlwaysInjectsDotnetCliUseMsBuildServerEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER"));
                Assert.Equal("true", env["DOTNET_CLI_USE_MSBUILD_SERVER"]);
            },
            0
            );

        var exitCode = await runner.BuildAsync(projectFile, options, CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task BuildAsyncUsesConfigurationValueForDotnetCliUseMsBuildServer()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        // Add a configuration value that overrides the default
        services.AddSingleton<IConfiguration>(sp =>
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "false"
            });
            return configBuilder.Build();
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER"));
                Assert.Equal("false", env["DOTNET_CLI_USE_MSBUILD_SERVER"]);
            },
            0
            );

        var exitCode = await runner.BuildAsync(projectFile, options, CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncInjectsDotnetCliUseMsBuildServerWhenNoBuildIsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER"));
                Assert.Equal("true", env["DOTNET_CLI_USE_MSBUILD_SERVER"]);
            },
            0
            );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false,
            noBuild: false, // This should inject the environment variable
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncDoesNotInjectDotnetCliUseMsBuildServerWhenNoBuildIsTrue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _) =>
            {
                // When noBuild is true, the original env should be passed through unchanged
                // or should be null if no env was provided
                if (env != null)
                {
                    Assert.False(env.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER"));
                }
            },
            0
            );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false,
            noBuild: true, // This should NOT inject the environment variable
            args: ["--operation", "inspect"],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncPreservesExistingEnvironmentVariables()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER"));
                Assert.Equal("true", env["DOTNET_CLI_USE_MSBUILD_SERVER"]);
                // Verify existing environment variable is preserved
                Assert.True(env.ContainsKey("EXISTING_VAR"));
                Assert.Equal("existing_value", env["EXISTING_VAR"]);
            },
            0
            );

        var existingEnv = new Dictionary<string, string>
        {
            ["EXISTING_VAR"] = "existing_value"
        };

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false,
            noBuild: false,
            args: ["--operation", "inspect"],
            env: existingEnv,
            null,
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_SetsExtensionPidFilePath_WhenApphostDebugCapabilityAndExtensionMode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => new TestExtensionInteractionService();
            options.ExtensionBackchannelFactory
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();
        var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.Contains("ASPIRE_EXTENSION_PID_PATH", env.Keys);
                Assert.False(string.IsNullOrWhiteSpace(env[KnownConfigNames.ExtensionPidFilePath]));
            },
            0
        );

        await runner.RunAsync(
            projectFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            backchannelCompletionSource,
            options,
            CancellationToken.None
        );
    }

    [Fact]
    public async Task RunAsync_DoesNotSetExtensionPidFilePath_IfNoApphostDebugCapabilityOrNotExtensionMode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        // No apphost-debug capability
        var services1 = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AppHostBackchannelFactory = _ => new FakeAppHostBackchannel([]);
            options.InteractionServiceFactory = _ => new DummyInteractionService();
        });
        var provider1 = services1.BuildServiceProvider();
        var logger1 = provider1.GetRequiredService<ILogger<DotNetCliRunner>>();
        var runner1 = new AssertingDotNetCliRunner(
            logger1,
            provider1,
            new AspireCliTelemetry(),
            provider1.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.DoesNotContain("ASPIRE_EXTENSION_PID_PATH", env.Keys);
            },
            0
        );
        await runner1.RunAsync(
            projectFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            new TaskCompletionSource<IAppHostBackchannel>(),
            new DotNetCliRunnerInvocationOptions(),
            CancellationToken.None
        );

        // Not in extension mode
        var services2 = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider2 = services2.BuildServiceProvider();
        var logger2 = provider2.GetRequiredService<ILogger<DotNetCliRunner>>();
        var runner2 = new AssertingDotNetCliRunner(
            logger2,
            provider2,
            new AspireCliTelemetry(),
            provider2.GetRequiredService<IConfiguration>(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.DoesNotContain(KnownConfigNames.ExtensionPidFilePath, env.Keys);
            },
            0
        );
        await runner2.RunAsync(
            projectFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            new DotNetCliRunnerInvocationOptions(),
            CancellationToken.None
        );
    }
}

internal sealed class AssertingDotNetCliRunner(
    ILogger<DotNetCliRunner> logger,
    IServiceProvider serviceProvider,
    AspireCliTelemetry telemetry,
    IConfiguration configuration,
    Action<string[], IDictionary<string, string>?, DirectoryInfo, FileInfo?, TaskCompletionSource<IAppHostBackchannel>?, DotNetCliRunnerInvocationOptions> assertionCallback,
    int exitCode
    ) : DotNetCliRunner(logger, serviceProvider, telemetry, configuration)
{
    public override Task<int> ExecuteAsync(string[] args, IDictionary<string, string>? env, FileInfo? projectFile, DirectoryInfo workingDirectory, TaskCompletionSource<IAppHostBackchannel>? backchannelCompletionSource, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        assertionCallback(args, env, workingDirectory, projectFile, backchannelCompletionSource, options);
        return Task.FromResult(exitCode);
    }
}
