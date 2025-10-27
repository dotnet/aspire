// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Caching;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.DotNet;

public class DotNetCliRunnerTests(ITestOutputHelper outputHelper)
{
    private static Aspire.Cli.CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory)
    {
        // NOTE: This would normally be in the users home directory, but for tests we create
        //       it in the temporary workspace directory.
        var settingsDirectory = workingDirectory.CreateSubdirectory(".aspire");
        var hivesDirectory = settingsDirectory.CreateSubdirectory("hives");
        var cacheDirectory = new DirectoryInfo(Path.Combine(workingDirectory.FullName, ".aspire", "cache"));
        return new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
    }

    [Fact]
    public async Task DotNetCliCorrectlyAppliesNoLaunchProfileArgumentWhenSpecifiedInOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = true
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) => Assert.Contains(args, arg => arg == "--no-launch-profile"),
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
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
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
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
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
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
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
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
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
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
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
    public async Task NewProjectAsyncReturnsExitCode73WhenProjectAlreadyExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
            {
                // Verify the arguments are correct for dotnet new
                Assert.Contains("new", args);
                Assert.Contains("aspire", args);
                Assert.Contains("--name", args);
                Assert.Contains("TestProject", args);
                Assert.Contains("--output", args);
                Assert.Contains("/tmp/test", args);
            },
            73 // Return exit code 73 to simulate project already exists
        );

        // Act
        var exitCode = await runner.NewProjectAsync("aspire", "TestProject", "/tmp/test", [], options, CancellationToken.None);

        // Assert
        Assert.Equal(73, exitCode);
    }

    [Fact]
    public async Task RunAsyncSetsVersionCheckDisabledWhenUpdateNotificationsFeatureIsDisabled()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.DisabledFeatures = [KnownFeatures.UpdateNotificationsEnabled];
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            provider.GetRequiredService<CliExecutionContext>(),
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("ASPIRE_VERSION_CHECK_DISABLED"));
                Assert.Equal("true", env["ASPIRE_VERSION_CHECK_DISABLED"]);
            },
            0
            );

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

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncDoesNotSetVersionCheckDisabledWhenUpdateNotificationsFeatureIsEnabled()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.UpdateNotificationsEnabled];
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            provider.GetRequiredService<CliExecutionContext>(),
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
            {
                // When the feature is enabled (default), the version check env var should NOT be set
                if (env != null)
                {
                    Assert.False(env.ContainsKey("ASPIRE_VERSION_CHECK_DISABLED"));
                }
            },
            0
            );

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

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncDoesNotOverrideUserProvidedVersionCheckDisabledValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.DisabledFeatures = [KnownFeatures.UpdateNotificationsEnabled];
        });
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();

        var options = new DotNetCliRunnerInvocationOptions();

        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            provider.GetRequiredService<IInteractionService>(),
            provider.GetRequiredService<CliExecutionContext>(),
            new NullDiskCache(),
            (args, env, _, _, _, _) =>
            {
                Assert.NotNull(env);
                Assert.True(env.ContainsKey("ASPIRE_VERSION_CHECK_DISABLED"));
                // Should preserve user's value, not override with "true"
                Assert.Equal("false", env["ASPIRE_VERSION_CHECK_DISABLED"]);
            },
            0
            );

        // User explicitly sets the environment variable to false
        var userEnv = new Dictionary<string, string>
        {
            ["ASPIRE_VERSION_CHECK_DISABLED"] = "false"
        };

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: false,
            noBuild: false,
            args: ["--operation", "inspect"],
            env: userEnv,
            null,
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ExecuteAsyncLaunchesAppHostInExtensionHostIfConnected()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var launchAppHostCalledTcs = new TaskCompletionSource();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = sp => new TestExtensionInteractionService(sp)
            {
                LaunchAppHostCallback = () => launchAppHostCalledTcs.SetResult(),
            };
            options.ConfigurationCallback = configBuilder =>
            {
                configBuilder["ASPIRE_EXTENSION_TOKEN"] = "extension-token";
            };
            options.ExtensionBackchannelFactory = _ => new TestExtensionBackchannel
            {
                HasCapabilityAsyncCallback = (c, _) => Task.FromResult(c is "devkit" or "project"),
            };
            options.AppHostBackchannelFactory = _ => new TestAppHostBackchannel();
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new DotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache()
        );

        var exitCode = await runner.ExecuteAsync(
            args: ["run", "--project", projectFile.FullName],
            env: null,
            projectFile: projectFile,
            workingDirectory: workspace.WorkspaceRoot,
            backchannelCompletionSource: new TaskCompletionSource<IAppHostBackchannel>(),
            options: new DotNetCliRunnerInvocationOptions(),
            cancellationToken: CancellationToken.None
        );

        await launchAppHostCalledTcs.Task;
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task AddPackageAsyncUseFilesSwitchForSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, "// Single-file AppHost");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // Verify arguments are correct for single-file AppHost
                Assert.Contains("add", args);
                Assert.Contains("package", args);
                Assert.Contains("--file", args);
                Assert.Contains(appHostFile.FullName, args);
                Assert.Contains("Aspire.Hosting.Redis@9.2.0", args);
                Assert.Contains("--no-restore", args);

                // Verify the order: add package PackageName --file FilePath --version Version --no-restore
                var addIndex = Array.IndexOf(args, "add");
                var packageIndex = Array.IndexOf(args, "package");
                var fileIndex = Array.IndexOf(args, "--file");
                var filePathIndex = Array.IndexOf(args, appHostFile.FullName);
                var packageNameIndex = Array.IndexOf(args, "Aspire.Hosting.Redis@9.2.0");

                Assert.True(addIndex < packageIndex);
                Assert.True(packageIndex < fileIndex);
                Assert.True(fileIndex < filePathIndex);
                Assert.True(filePathIndex < packageNameIndex);
            },
            0
            );

        var exitCode = await runner.AddPackageAsync(
            appHostFile,
            "Aspire.Hosting.Redis",
            "9.2.0",
            null, // no source, should use --no-restore
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AddPackageAsyncUsesPositionalArgumentForCsprojFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // Verify arguments are correct for .csproj file (new behavior with --version)
                Assert.Contains("add", args);
                Assert.Contains("package", args);
                Assert.Contains(projectFile.FullName, args);
                Assert.Contains("Aspire.Hosting.Redis", args);
                Assert.Contains("--version", args);
                Assert.Contains("9.2.0", args);
                Assert.Contains("--source", args);
                Assert.Contains("https://api.nuget.org/v3/index.json", args);

                // Verify the order: add ProjectFile package PackageName --version Version --source Source
                var addIndex = Array.IndexOf(args, "add");
                var projectIndex = Array.IndexOf(args, projectFile.FullName);
                var packageIndex = Array.IndexOf(args, "package");
                var packageNameIndex = Array.IndexOf(args, "Aspire.Hosting.Redis");
                var versionFlagIndex = Array.IndexOf(args, "--version");
                var versionValueIndex = Array.IndexOf(args, "9.2.0");

                Assert.True(addIndex < projectIndex);
                Assert.True(projectIndex < packageIndex);
                Assert.True(packageIndex < packageNameIndex);
                Assert.True(packageNameIndex < versionFlagIndex);
                Assert.True(versionFlagIndex < versionValueIndex);

                // Should NOT contain --file or the @version format
                Assert.DoesNotContain("--file", args);
                Assert.DoesNotContain("Aspire.Hosting.Redis@9.2.0", args);
            },
            0
            );

        var exitCode = await runner.AddPackageAsync(
            projectFile,
            "Aspire.Hosting.Redis",
            "9.2.0",
            "https://api.nuget.org/v3/index.json", // provide source, should use --source
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AddPackageAsyncUsesPositionalArgumentForCsprojFileWithNoRestore()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "<Project></Project>");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // Verify arguments are correct for .csproj file with --no-restore (no source provided)
                Assert.Contains("add", args);
                Assert.Contains("package", args);
                Assert.Contains(projectFile.FullName, args);
                Assert.Contains("Aspire.Hosting.Redis", args);
                Assert.Contains("--version", args);
                Assert.Contains("9.2.0", args);
                Assert.Contains("--no-restore", args);

                // Verify the order: add ProjectFile package PackageName --version Version --no-restore
                var addIndex = Array.IndexOf(args, "add");
                var projectIndex = Array.IndexOf(args, projectFile.FullName);
                var packageIndex = Array.IndexOf(args, "package");
                var packageNameIndex = Array.IndexOf(args, "Aspire.Hosting.Redis");
                var versionFlagIndex = Array.IndexOf(args, "--version");
                var versionValueIndex = Array.IndexOf(args, "9.2.0");
                var noRestoreIndex = Array.IndexOf(args, "--no-restore");

                Assert.True(addIndex < projectIndex);
                Assert.True(projectIndex < packageIndex);
                Assert.True(packageIndex < packageNameIndex);
                Assert.True(packageNameIndex < versionFlagIndex);
                Assert.True(versionFlagIndex < versionValueIndex);
                Assert.True(versionValueIndex < noRestoreIndex);

                // Should NOT contain --file, --source, or the @version format
                Assert.DoesNotContain("--file", args);
                Assert.DoesNotContain("--source", args);
                Assert.DoesNotContain("Aspire.Hosting.Redis@9.2.0", args);
            },
            0
            );

        var exitCode = await runner.AddPackageAsync(
            projectFile,
            "Aspire.Hosting.Redis",
            "9.2.0",
            null, // no source, should use --no-restore
            options,
            CancellationToken.None
            );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task GetSolutionProjectsAsync_ParsesOutputCorrectly()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a fake solution file
        var solutionFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "Test.sln"));
        await File.WriteAllTextAsync(solutionFile.FullName, "Not a real solution file.");

        // Create project files
        var project1Dir = workspace.WorkspaceRoot.CreateSubdirectory("Project1");
        var project1File = new FileInfo(Path.Combine(project1Dir.FullName, "Project1.csproj"));
        await File.WriteAllTextAsync(project1File.FullName, "Not a real project file.");

        var project2Dir = workspace.WorkspaceRoot.CreateSubdirectory("Project2");
        var project2File = new FileInfo(Path.Combine(project2Dir.FullName, "Project2.csproj"));
        await File.WriteAllTextAsync(project2File.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = (line) => outputHelper.WriteLine($"stdout: {line}")
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, invocationOptions) =>
            {
                // Simulate dotnet sln list output
                invocationOptions.StandardOutputCallback?.Invoke("Project(s)");
                invocationOptions.StandardOutputCallback?.Invoke("----------");
                invocationOptions.StandardOutputCallback?.Invoke($"Project1{Path.DirectorySeparatorChar}Project1.csproj");
                invocationOptions.StandardOutputCallback?.Invoke($"Project2{Path.DirectorySeparatorChar}Project2.csproj");
            },
            0
        );

        var (exitCode, projects) = await runner.GetSolutionProjectsAsync(solutionFile, options, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Name == "Project1.csproj");
        Assert.Contains(projects, p => p.Name == "Project2.csproj");
    }

    [Fact]
    public async Task AddProjectReferenceAsync_ExecutesCorrectCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var referencedProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "Service.csproj"));
        await File.WriteAllTextAsync(referencedProject.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                Assert.Contains("add", args);
                Assert.Contains(projectFile.FullName, args);
                Assert.Contains("reference", args);
                Assert.Contains(referencedProject.FullName, args);
            },
            0
        );

        var exitCode = await runner.AddProjectReferenceAsync(projectFile, referencedProject, options, CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncAppliesNoLaunchProfileForSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, "// Single-file AppHost");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = true
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // For single-file .cs files, should include --no-launch-profile
                Assert.Collection(args,
                    arg => Assert.Equal("run", arg),
                    arg => Assert.Equal("--no-launch-profile", arg),
                    arg => Assert.Equal("--file", arg),
                    arg => Assert.Equal(appHostFile.FullName, arg),
                    arg => Assert.Equal("--", arg)
                );
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: appHostFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncDoesNotIncludeNoLaunchProfileForSingleFileAppHostWhenNotSpecified()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, "// Single-file AppHost");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = false
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // For single-file .cs files, should NOT include --no-launch-profile when false
                Assert.Collection(args,
                    arg => Assert.Equal("run", arg),
                    arg => Assert.Equal("--file", arg),
                    arg => Assert.Equal(appHostFile.FullName, arg),
                    arg => Assert.Equal("--", arg)
                );
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: appHostFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncFiltersOutEmptyAndWhitespaceArguments()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        // Use watch=true and NoLaunchProfile=false to ensure some empty strings are generated
        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = false,
            Debug = false
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // Verify no empty or whitespace-only arguments exist
                foreach (var arg in args)
                {
                    Assert.False(string.IsNullOrWhiteSpace(arg), $"Found empty or whitespace argument in args: [{string.Join(", ", args)}]");
                }
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true, // This will generate empty strings for verboseSwitch when Debug=false
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncFiltersOutEmptyArgumentsForSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, "// Single-file AppHost");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = false // This will generate an empty string for noProfileSwitch
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // Verify no empty or whitespace-only arguments exist in single-file AppHost scenario
                foreach (var arg in args)
                {
                    Assert.False(string.IsNullOrWhiteSpace(arg), $"Found empty or whitespace argument in args: [{string.Join(", ", args)}]");
                }

                // Ensure the correct arguments are present
                Assert.Collection(args,
                    arg => Assert.Equal("run", arg),
                    arg => Assert.Equal("--file", arg),
                    arg => Assert.Equal(appHostFile.FullName, arg),
                    arg => Assert.Equal("--", arg)
                );
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: appHostFile,
            watch: false,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncIncludesAllNonEmptyFlagsWhenEnabled()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = true,
            Debug = true
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // With watch=true and Debug=true, should include --verbose
                Assert.Collection(args,
                    arg => Assert.Equal("watch", arg),
                    arg => Assert.Equal("--non-interactive", arg),
                    arg => Assert.Equal("--verbose", arg),
                    arg => Assert.Equal("--no-launch-profile", arg),
                    arg => Assert.Equal("--project", arg),
                    arg => Assert.Equal(projectFile.FullName, arg),
                    arg => Assert.Equal("--", arg)
                );
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsyncCorrectlyHandlesWatchWithoutDebug()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions()
        {
            NoLaunchProfile = true,
            Debug = false // No debug, so no --verbose
        };

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, _) =>
            {
                // With watch=true but Debug=false, should NOT include --verbose
                Assert.Collection(args,
                    arg => Assert.Equal("watch", arg),
                    arg => Assert.Equal("--non-interactive", arg),
                    arg => Assert.Equal("--no-launch-profile", arg),
                    arg => Assert.Equal("--project", arg),
                    arg => Assert.Equal(projectFile.FullName, arg),
                    arg => Assert.Equal("--", arg)
                );
            },
            0
        );

        var exitCode = await runner.RunAsync(
            projectFile: projectFile,
            watch: true,
            noBuild: false,
            args: [],
            env: new Dictionary<string, string>(),
            null,
            options,
            CancellationToken.None
        );

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task GetProjectItemsAndPropertiesAsync_UsesBuild_ForSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, "// Single-file AppHost");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, invocationOptions) =>
            {
                // Verify that "build" command is used for single-file app host
                Assert.Contains("build", args);
                Assert.DoesNotContain("msbuild", args);
                
                // Provide valid JSON output
                invocationOptions.StandardOutputCallback?.Invoke("{\"Properties\":{\"MSBuildVersion\":\"17.0.0\",\"AspireHostingSDKVersion\":\"9.0.0\"},\"Items\":{\"PackageReference\":[]}}");
            },
            0
        );

        await runner.GetProjectItemsAndPropertiesAsync(
            appHostFile,
            ["PackageReference"],
            ["AspireHostingSDKVersion"],
            options,
            CancellationToken.None
        );
    }

    [Fact]
    public async Task GetProjectItemsAndPropertiesAsync_UsesMsBuild_ForCsProjFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var options = new DotNetCliRunnerInvocationOptions();

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var runner = new AssertingDotNetCliRunner(
            logger,
            provider,
            new AspireCliTelemetry(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IFeatures>(),
            interactionService,
            executionContext,
            new NullDiskCache(),
            (args, _, _, _, _, invocationOptions) =>
            {
                // Verify that "msbuild" command is used for .csproj files
                Assert.Contains("msbuild", args);
                Assert.DoesNotContain("build", args);
                
                // Provide valid JSON output
                invocationOptions.StandardOutputCallback?.Invoke("{\"Properties\":{\"MSBuildVersion\":\"17.0.0\",\"AspireHostingSDKVersion\":\"9.0.0\"},\"Items\":{\"PackageReference\":[]}}");
            },
            0
        );

        await runner.GetProjectItemsAndPropertiesAsync(
            projectFile,
            ["PackageReference"],
            ["AspireHostingSDKVersion"],
            options,
            CancellationToken.None
        );
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
