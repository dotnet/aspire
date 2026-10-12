// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Bundles;
using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.Agents;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.Cli.Tests.Commands;

// `SetupCommand.GetDefaultInstallPath` returns parent-of-binary regardless of
// install route — distinct from the route-aware `BundleService.GetDefaultExtractDir`
// used by auto-extract. These tests pin the route-independent contract.
public class SetupCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Setup_RegistersOnlyUserPluginsAndManualInitReusesTheSameConfiguration()
    {
        using var context = new AgentConfigurationTestContext(output);
        using var provider = CreateProvider(context);
        var root = provider.GetRequiredService<RootCommand>();

        var setupExitCode = await root.Parse(
            $"setup --install-path \"{context.Workspace.WorkspaceRoot.FullName}\" --aspire-skills y --agents all --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, setupExitCode);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        string[] paths =
        [
            Path.Combine(context.CopilotDirectory, "settings.json"),
            Path.Combine(context.ClaudeDirectory, "settings.json"),
            Path.Combine(context.OpenCodeDirectory, "opencode.json")
        ];
        var contents = new List<byte[]>();
        var timestamps = new List<DateTime>();
        foreach (var path in paths)
        {
            contents.Add(await File.ReadAllBytesAsync(path).DefaultTimeout());
            File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            timestamps.Add(File.GetLastWriteTimeUtc(path));
        }

        var initExitCode = await root.Parse(
            "agent init --scope global --aspire-skills y --mcp n --playwright n --dotnet-inspect n --agents all --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, initExitCode);
        for (var index = 0; index < paths.Length; index++)
        {
            Assert.Equal(contents[index], await File.ReadAllBytesAsync(paths[index]).DefaultTimeout());
            Assert.Equal(timestamps[index], File.GetLastWriteTimeUtc(paths[index]));
        }
        Assert.Equal(4, context.Home.EnumerateFiles("*", SearchOption.AllDirectories).Count());
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
        Assert.Empty(context.Interaction.FilePathPromptCalls);
    }

    [Theory]
    [InlineData("NoPayload")]
    [InlineData("AlreadyUpToDate")]
    [InlineData("Extracted")]
    public async Task SuccessfulRuntimeSetup_OffersExplicitPluginRegistration(string extraction)
    {
        using var context = new AgentConfigurationTestContext(output);
        var installPath = context.Workspace.CreateDirectory("installation").FullName;
        var bundle = new TestBundleService(isBundle: true)
        {
            ExtractAsyncCallback = (destination, force, _) =>
            {
                Assert.Equal(installPath, destination);
                Assert.True(force);
                return Task.FromResult(Enum.Parse<BundleExtractResult>(extraction));
            }
        };
        using var provider = CreateProvider(context, bundle);

        var exitCode = await provider.GetRequiredService<RootCommand>().Parse(
            $"setup --install-path \"{installPath}\" --force --aspire-skills y --agents \"GitHub Copilot\" --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.True(File.Exists(Path.Combine(context.CopilotDirectory, "settings.json")));
        Assert.Equal("true", (await context.Configuration.GetGlobalConfigurationAsync(throwOnError: true).DefaultTimeout())[CliConfigNames.AgentSetupCompleted]);
    }

    [Fact]
    public async Task FailedRuntimeExtraction_DoesNotOfferOrConfigurePlugins()
    {
        using var context = new AgentConfigurationTestContext(output);
        var bundle = new TestBundleService(isBundle: true)
        {
            ExtractAsyncCallback = (_, _, _) => Task.FromResult(BundleExtractResult.ExtractionFailed)
        };
        using var provider = CreateProvider(context, bundle);

        var exitCode = await provider.GetRequiredService<RootCommand>().Parse(
            $"setup --install-path \"{context.Workspace.WorkspaceRoot.FullName}\" --aspire-skills y --agents all --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.FailedToBuildArtifacts, exitCode);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.Interaction.BooleanPromptCalls);
    }

    [Fact]
    public async Task OptionalPluginFailure_PreservesSuccessfulSetupButManualInitReturnsFailure()
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = Path.Combine(context.CopilotDirectory, "settings.json");
        const string existing = "{broken";
        await AgentConfigurationTestContext.WriteAsync(path, existing).DefaultTimeout();
        using var provider = CreateProvider(context);
        var root = provider.GetRequiredService<RootCommand>();

        var setupExitCode = await root.Parse(
            $"setup --install-path \"{context.Workspace.WorkspaceRoot.FullName}\" --aspire-skills y --agents \"GitHub Copilot\" --non-interactive")
            .InvokeAsync().DefaultTimeout();
        var initExitCode = await root.Parse(
            "agent init --scope global --aspire-skills y --mcp n --playwright n --dotnet-inspect n --agents \"GitHub Copilot\" --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, setupExitCode);
        Assert.Equal(CliExitCodes.InvalidCommand, initExitCode);
        Assert.Single(context.Interaction.DisplayedMessages, message => message.Message == AgentCommandStrings.SetupCommand_PluginRetry);
        Assert.Empty(context.Interaction.DisplayedSuccess);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
    }

    [Fact]
    public async Task HeadlessSetupWithoutConsent_DoesNotConsumeOnboarding()
    {
        using var context = new AgentConfigurationTestContext(output);
        using var provider = CreateProvider(context);

        var exitCode = await provider.GetRequiredService<RootCommand>().Parse(
            $"setup --install-path \"{context.Workspace.WorkspaceRoot.FullName}\" --non-interactive")
            .InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Interaction.BooleanPromptCalls);
    }

    [Fact]
    public void GetDefaultInstallPath_ScriptRouteLayout_ReturnsParentOfBinaryDirectory()
    {
        var processPath = Path.Combine("home", ".aspire", "bin", "aspire");

        var result = SetupCommand.GetDefaultInstallPath(processPath);

        Assert.Equal(Path.Combine("home", ".aspire"), result);
    }

    [Fact]
    public void GetDefaultInstallPath_PRRouteLayout_ReturnsParentOfBinaryDirectory()
    {
        var processPath = Path.Combine("home", ".aspire", "dogfood", "pr-12345", "bin", "aspire");

        var result = SetupCommand.GetDefaultInstallPath(processPath);

        Assert.Equal(Path.Combine("home", ".aspire", "dogfood", "pr-12345"), result);
    }

    [Fact]
    public void GetDefaultInstallPath_ManagedRouteFlatLayout_ReturnsParentOfBinaryDirectory()
    {
        // Managed-route layout is flat (no bin/ subdir). `BundleService.GetDefaultExtractDir`
        // diverges here and keeps the payload inside the binary's dir so package-manager uninstall reaches it.
        var processPath = Path.Combine("Program Files", "WindowsApps", "Microsoft.Aspire_xyz", "aspire.exe");

        var result = SetupCommand.GetDefaultInstallPath(processPath);

        Assert.Equal(Path.Combine("Program Files", "WindowsApps"), result);
    }

    [Fact]
    public void GetDefaultInstallPath_BareFilename_ReturnsNull()
    {
        var result = SetupCommand.GetDefaultInstallPath("aspire");

        Assert.Null(result);
    }

    [Fact]
    public void GetDefaultInstallPath_EmptyProcessPath_ReturnsNull()
    {
        var result = SetupCommand.GetDefaultInstallPath(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void GetDefaultInstallPath_NullProcessPath_ReturnsNull()
    {
        var result = SetupCommand.GetDefaultInstallPath(null);

        Assert.Null(result);
    }

    private ServiceProvider CreateProvider(AgentConfigurationTestContext context, TestBundleService? bundle = null)
    {
        var services = CliTestHelper.CreateServiceCollection(context.Workspace, output, options =>
        {
            // The helper creates CLI logs here; keep them outside the project under observation.
            options.WorkingDirectory = context.Workspace.WorkspaceRoot;
            options.CliExecutionContextFactory = _ => context.ExecutionContext;
            options.ConfigurationServiceFactory = _ => context.Configuration;
            options.InteractionServiceFactory = _ => context.Interaction;
            options.AgentSkillInstallerFactory = _ => context.SkillInstaller;
            options.TelemetryHookConfiguratorFactory = _ => context.Hooks;
            options.BundleServiceFactory = _ => bundle ?? new TestBundleService(isBundle: true);
            options.GitRepositoryFactory = _ => new TestGitRepository
            {
                GetRootAsyncCallback = _ => throw new InvalidOperationException("User-only plugin setup must not discover a workspace.")
            };
        });
        services.RemoveAll<IAgentEnvironmentScanner>();
        foreach (var scanner in context.Environments)
        {
            services.AddSingleton(scanner);
        }
        services.RemoveAll<IEnvironment>();
        services.AddSingleton<IEnvironment>(context.Environment);

        return services.BuildServiceProvider();
    }
}
