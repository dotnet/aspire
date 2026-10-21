// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.AspireSkills;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Npm;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.Agents;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class AgentInitCommandTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("agent init")]
    [InlineData("mcp init")]
    [InlineData("new aspire-empty")]
    [InlineData("init")]
    public void AgentInitOptions_SelectSkillsAndOneAspireSource(string command)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var defaults = root.Parse(command);
        Assert.Empty(defaults.Errors);
        Assert.Equal(AspireSkillsMode.Embedded, defaults.GetValue<AspireSkillsMode>("--aspire-source"));

        foreach (var (source, mode) in new[] { ("embedded", AspireSkillsMode.Embedded), ("native", AspireSkillsMode.Native) })
        {
            var result = root.Parse($"{command} --skills aspire,playwright-cli,dotnet-inspect --aspire-source {source.ToUpperInvariant()}");
            Assert.Empty(result.Errors);
            Assert.Equal(mode, result.GetValue<AspireSkillsMode>("--aspire-source"));
        }

        foreach (var skills in new[] { "aspire", "playwright-cli", "PLAYWRIGHT-CLI", "dotnet-inspect", "ASPIRE,aspire,Playwright-CLI", "all", "none" })
        {
            Assert.Empty(root.Parse($"{command} --skills {skills}").Errors);
        }

        foreach (var options in new[]
        {
            "--aspire-source", "--aspire-source none", "--aspire-source bundled", "--aspire-source true", "--aspire-source 0",
            "--aspire-source native,embedded", "--aspire-source native --aspire-source embedded",
            "--skills", "--skills unsupported", "--skills aspire,none", "--skills mcp", "--skills playwright", "--skills \"\"",
            "--aspire-skills native", "--playwright", "--dotnet-inspect"
        })
        {
            Assert.NotEmpty(root.Parse($"{command} {options}").Errors);
        }

        Assert.Equal(["all", "aspire", "dotnet-inspect", "none", "playwright-cli"],
            root.Parse($"{command} --skills ").GetCompletions().Select(item => item.Label).Order(StringComparer.Ordinal));
        Assert.Equal(["embedded", "native"],
            root.Parse($"{command} --aspire-source ").GetCompletions().Select(item => item.Label).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" true", true)]
    [InlineData("=TRUE", true)]
    [InlineData(" false", false)]
    [InlineData("=false", false)]
    public void McpOption_RemainsAnIndependentConfirmation(string suffix, bool expected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var provider = CliTestHelper.CreateServiceCollection(workspace, outputHelper).BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse($"agent init --mcp{suffix}");

        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.GetValue<bool?>("--mcp"));
        Assert.NotEmpty(root.Parse("agent init --mcp invalid").Errors);
        Assert.NotEmpty(root.Parse($"new aspire-empty --mcp{suffix}").Errors);
        Assert.NotEmpty(root.Parse($"init --mcp{suffix}").Errors);
    }

    [Theory]
    [InlineData("aspire", true)]
    [InlineData("aspire,dotnet-inspect", true)]
    [InlineData("dotnet-inspect", false)]
    [InlineData("none", false)]
    public async Task AgentInit_PromptsForSourceOnlyWhenAspireIsSelected(string skills, bool expectSourcePrompt)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        await File.WriteAllTextAsync(Path.Combine(context.Project.FullName, "apphost.cs"), string.Empty);
        var sourcePrompts = 0;
        var riskWarning = string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_PluginRiskWarning,
            AspireSkillsConfiguration.RepositoryUrl);
        var interaction = new TestInteractionService();
        interaction.PromptForSelectionCallback = (prompt, choices, formatter, _) =>
        {
            Assert.Equal(AgentCommandStrings.InitCommand_ConfigureAspireSourcePrompt, prompt);
            Assert.Equal([AspireSkillsMode.Embedded, AspireSkillsMode.Native], choices.Cast<AspireSkillsMode>());
            Assert.Equal([AgentCommandStrings.InitCommand_EmbeddedSkillsChoice, AgentCommandStrings.InitCommand_NativeSkillsChoice],
                choices.Cast<object>().Select(formatter));
            Assert.Equal(riskWarning, Assert.Single(interaction.DisplayedMessages).Message);
            sourcePrompts++;
            return AspireSkillsMode.Native;
        };
        var services = context.CreateServiceCollection(outputHelper, options =>
        {
            options.CliHostEnvironmentFactory = _ => TestHelpers.CreateInteractiveHostEnvironment();
            options.InteractionServiceFactory = _ => interaction;
            options.AspireSkillsInstallerFactory = _ => new FakeAspireSkillsInstaller(context.ExecutionContext,
                AspireSkillsInstallResult.Failed("Only embedded source may acquire a bundle."));
        });
        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents copilot --scope project --skills {skills}");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(expectSourcePrompt ? 1 : 0, sourcePrompts);
        Assert.Equal(expectSourcePrompt ? [riskWarning] : [],
            interaction.DisplayedMessages.Where(message => message.Emoji.Equals(KnownEmojis.Warning)).Select(message => message.Message));
        Assert.Equal(expectSourcePrompt, File.Exists(context.Copilot.GetIntegrationConfigPath(AgentConfigurationScope.Project, context.Project)));
        Assert.False(File.Exists(Path.Combine(context.Project.FullName, ".mcp.json")));
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData("native")]
    [InlineData("embedded")]
    public async Task AgentInit_ExplicitSourceDoesNotEnableUnselectedAspire(string source)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var hooks = new TestTelemetryHookConfigurator();
        var interaction = new TestInteractionService
        {
            PromptForSelectionCallback = (_, _, _, _) => throw new InvalidOperationException("No source or scope prompt is expected.")
        };
        var services = context.CreateServiceCollection(outputHelper, options =>
        {
            options.CliHostEnvironmentFactory = _ => TestHelpers.CreateInteractiveHostEnvironment();
            options.InteractionServiceFactory = _ => interaction;
            options.TelemetryHookConfiguratorFactory = _ => hooks;
            options.AspireSkillsInstallerFactory = _ => new FakeAspireSkillsInstaller(context.ExecutionContext,
                AspireSkillsInstallResult.Failed("Unselected Aspire must not acquire a bundle."));
        });
        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents copilot --scope global --skills none --aspire-source {source}");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Equal(AgentEnvironment.None, hooks.ConfiguredEnvironments);
        Assert.Empty(interaction.DisplayedErrors);
        Assert.Empty(interaction.DisplayedMessages);
    }

    [Fact]
    public async Task AgentInitCommand_SummarizesNormalizedDisplayPath_WhenInstallingUserLevelSkill()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var interactionService = new TestInteractionService();
        interactionService.SetupStringPromptResponse(workspace.WorkspaceRoot.FullName);
        interactionService.PromptForSelectionsCallback = (_, choices, _, _) => choices.Cast<object>()
            .Where(choice => choice switch
            {
                IAgentClient client => client.Id == "copilot",
                string name => name == CommonAgentApplicators.AspireSkillName,
                _ => false
            })
            .ToList();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.CliExecutionContextFactory = _ => CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory);
        });

        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("agent init --agents copilot --scope global");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(0, exitCode);
        var expectedSummary = string.Join(Environment.NewLine,
            AgentCommandStrings.InitCommand_InstalledSkillsSummary,
            $"  {string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InstalledSkillsSummarySkills,
                $"{CommonAgentApplicators.AspireSkillName}, {CommonAgentApplicators.AspireDeploymentSkillName}, {FakeAspireSkillsInstaller.AspireInitSkillName}, {FakeAspireSkillsInstaller.AspireMonitoringSkillName}, {FakeAspireSkillsInstaller.AspireOrchestrationSkillName}, {FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName}, {CommonAgentApplicators.AspireifySkillName}")}",
            $"  {string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InstalledSkillsSummaryLocations, "~/.agents/skills")}");

        Assert.Contains(
            interactionService.DisplayedMessages,
            displayedMessage => displayedMessage.Emoji.Equals(KnownEmojis.Robot) && displayedMessage.Message == expectedSummary);
        Assert.DoesNotContain(
            interactionService.DisplayedMessages,
            displayedMessage => displayedMessage.Message.Contains("Installed aspire skill", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AgentInitCommand_SummarizesDefaultSkillsOnce()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var interactionService = new TestInteractionService();
        interactionService.SetupStringPromptResponse(workspace.WorkspaceRoot.FullName);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.CliExecutionContextFactory = _ => CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory);
        });

        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("agent init --agents copilot");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(0, exitCode);

        var expectedSummary = string.Join(Environment.NewLine,
            AgentCommandStrings.InitCommand_InstalledSkillsSummary,
            $"  {string.Format(
                CultureInfo.CurrentCulture,
                AgentCommandStrings.InitCommand_InstalledSkillsSummarySkills,
                $"{CommonAgentApplicators.AspireSkillName}, {CommonAgentApplicators.AspireDeploymentSkillName}, {FakeAspireSkillsInstaller.AspireInitSkillName}, {FakeAspireSkillsInstaller.AspireMonitoringSkillName}, {FakeAspireSkillsInstaller.AspireOrchestrationSkillName}, {FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName}, {CommonAgentApplicators.AspireifySkillName}")}",
            $"  {string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InstalledSkillsSummaryLocations, ".agents/skills")}");
        var message = Assert.Single(interactionService.DisplayedMessages, displayedMessage => displayedMessage.Emoji.Equals(KnownEmojis.Robot));
        Assert.Equal(expectedSummary, message.Message);
        Assert.DoesNotContain(
            interactionService.DisplayedMessages,
            displayedMessage => displayedMessage.Message.Contains("Installed aspire skill", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AgentInitCommand_IncludesSpecificSkillDirectory_WhenInstallFails()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var blockedSkillParentPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents");
        await File.WriteAllTextAsync(blockedSkillParentPath, "blocked").DefaultTimeout();

        var interactionService = new TestInteractionService();
        interactionService.SetupStringPromptResponse(workspace.WorkspaceRoot.FullName);
        interactionService.PromptForSelectionsCallback = (_, choices, _, _) => choices.Cast<object>()
            .Where(choice => choice switch
            {
                IAgentClient client => client.Id == "copilot",
                string name => name == CommonAgentApplicators.AspireSkillName,
                _ => false
            })
            .ToList();
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
        });

        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("agent init");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Empty(interactionService.ValidationFailures);

        var expectedSkillDirectoryPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", CommonAgentApplicators.AspireSkillName);
        Assert.Contains(
            interactionService.DisplayedErrors,
            message => message.Contains(expectedSkillDirectoryPath, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithAllClientsAndSkills_InstallsSkillFiles()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all --skills all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Exit code is InvalidCommand because FakeNpmRunner cannot resolve Playwright CLI in tests.
        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.All(provider.GetServices<IAgentClient>(), client =>
            Assert.False(File.Exists(client.GetMcpConfigPath(AgentConfigurationScope.Project, workspace.WorkspaceRoot))));

        var expectedSkillNames = new[]
        {
            CommonAgentApplicators.AspireSkillName,
            CommonAgentApplicators.AspireifySkillName,
            CommonAgentApplicators.AspireDeploymentSkillName,
            FakeAspireSkillsInstaller.AspireInitSkillName,
            FakeAspireSkillsInstaller.AspireMonitoringSkillName,
            FakeAspireSkillsInstaller.AspireOrchestrationSkillName,
            FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName
        };
        var expectedSkillDirectories = new[]
        {
            Path.Combine(".agents", "skills"),
            Path.Combine(".claude", "skills")
        };

        foreach (var relativeSkillDirectory in expectedSkillDirectories)
        {
            foreach (var skillName in expectedSkillNames)
            {
                AssertSkillFileExists(workspace.WorkspaceRoot, relativeSkillDirectory, skillName);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AgentInitCommand_NonInteractive_InstallsProjectV2MigrationSkillWithReferences(bool explicitBundled)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory);
        });
        using var provider = services.BuildServiceProvider();

        var installer = provider.GetRequiredService<IAspireSkillsInstaller>();
        var installation = await installer.InstallAsync(TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.NotNull(installation.Bundle);
        var skill = Assert.Single(
            installation.Bundle.GetSkillDefinitions(),
            skill => skill.HasName(FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName));
        Assert.True(skill.IsDefault);
        Assert.Empty(skill.ApplicableLanguages);
        var expectedFiles = await installation.Bundle.GetSkillFilesAsync(skill, TestContext.Current.CancellationToken).DefaultTimeout();
        Assert.Equal(
            [
                "SKILL.md",
                Path.Combine("references", "compatibility-and-validation.md"),
                Path.Combine("references", "migration-patterns.md")
            ],
            expectedFiles.Select(file => file.RelativePath));

        var command = provider.GetRequiredService<RootCommand>();
        var selection = explicitBundled ? " --skills aspire --aspire-source embedded" : string.Empty;
        var result = command.Parse($"agent init --workspace-root \"{workspace.WorkspaceRoot.FullName}\" --agents all --scope project{selection}");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        var skillDirectories = new[]
        {
            Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", skill.Name),
            Path.Combine(workspace.WorkspaceRoot.FullName, ".claude", "skills", skill.Name)
        };
        foreach (var skillDirectory in skillDirectories)
        {
            Assert.Equal(
                expectedFiles.Select(file => file.RelativePath),
                Directory.EnumerateFiles(skillDirectory, "*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(skillDirectory, path))
                    .Order(StringComparer.Ordinal));
            foreach (var file in expectedFiles)
            {
                Assert.Equal(
                    file.Content,
                    await File.ReadAllTextAsync(Path.Combine(skillDirectory, file.RelativePath), TestContext.Current.CancellationToken).DefaultTimeout());
            }
        }
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithCliDefinedSkillDifferentCasing_DoesNotResolveBundle()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string installFailureMessage = "Aspire skills bundle is unavailable.";
        var interactionService = new TestInteractionService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.NpmRunnerFactory = _ => new FakeNpmRunner { IsAvailable = false };
            options.AspireSkillsInstallerFactory = serviceProvider => new FakeAspireSkillsInstaller(
                serviceProvider.GetRequiredService<CliExecutionContext>(),
                AspireSkillsInstallResult.Failed(installFailureMessage));
        });
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all --skills PLAYWRIGHT-CLI");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Empty(interactionService.DisplayedErrors);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithNoClients_SucceedsWithNoSkillsInstalled()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents none --skills all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);

        // No clients selected, so no skill directories should be created.
        var agentsDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents");
        Assert.False(Directory.Exists(agentsDir), $"Expected no .agents directory but found {agentsDir}");
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithoutClientSelection_UsesDefaultClients()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --skills none");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithoutSkills_UsesDefaultSkills()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Default Aspire skills are installed (every bundle skill, including aspireify).
        // Playwright is a CLI-defined skill that is not default.
        Assert.Equal(CliExitCodes.Success, exitCode);

        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireDeploymentSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireInitSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireMonitoringSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireOrchestrationSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireifySkillName);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithInvalidClient_FailsValidation()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents invalid --skills all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.NotEqual(CliExitCodes.Success, exitCode);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithoutWorkspaceRoot_UsesWorkingDirectory()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("agent init --agents copilot");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);

        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireDeploymentSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireInitSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireMonitoringSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireOrchestrationSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireifySkillName);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithUnavailableAspireSkillsBundle_FailsExplicitly()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string installFailureMessage = "Aspire skills bundle is unavailable.";
        var interactionService = new TestInteractionService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.AspireSkillsInstallerFactory = serviceProvider => new FakeAspireSkillsInstaller(
                serviceProvider.GetRequiredService<CliExecutionContext>(),
                AspireSkillsInstallResult.Failed(installFailureMessage));
        });
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, exitCode);
        Assert.Equal(installFailureMessage, Assert.Single(interactionService.DisplayedErrors));
    }

    [Fact]
    public async Task AgentInitCommand_UnavailableBundle_DoesNotPreventIndependentToolsOrMcp()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "apphost.cs"), string.Empty);
        const string installFailureMessage = "Aspire skills bundle is unavailable.";
        var interactionService = new TestInteractionService();
        var playwrightAttempted = false;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.AspireSkillsInstallerFactory = provider => new FakeAspireSkillsInstaller(
                provider.GetRequiredService<CliExecutionContext>(), AspireSkillsInstallResult.Failed(installFailureMessage));
        });
        services.AddSingleton<INpmRunner>(new FakeNpmRunner
        {
            ResolvePackageAsyncCallback = (_, _, _) =>
            {
                playwrightAttempted = true;
                return Task.FromResult<NpmPackageInfo?>(null);
            }
        });
        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<RootCommand>().Parse(
            "agent init --agents copilot --scope project --skills all --aspire-source embedded --mcp");

        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Contains(installFailureMessage, interactionService.DisplayedErrors);
        Assert.True(playwrightAttempted);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), SkillDefinition.DotnetInspect.Name);
        Assert.False(Directory.Exists(Path.Combine(workspace.Path, ".agents", "skills", CommonAgentApplicators.AspireSkillName)));
        Assert.True(File.Exists(Path.Combine(workspace.Path, ".mcp.json")));
    }

    [Fact]
    public async Task PromptAndChainAsync_WithUnavailableAspireSkillsBundle_FailsExplicitly()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        const string installFailureMessage = "Aspire skills bundle is unavailable.";
        var interactionService = new TestInteractionService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector { DetectedEnvironments = AgentEnvironment.CopilotCli };
            options.AspireSkillsInstallerFactory = serviceProvider => new FakeAspireSkillsInstaller(
                serviceProvider.GetRequiredService<CliExecutionContext>(),
                AspireSkillsInstallResult.Failed(installFailureMessage));
        });
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AgentInitCommand>();

        var result = await command.PromptAndChainAsync(
            interactionService,
            CliExitCodes.Success,
            workspace.WorkspaceRoot,
            PromptBinding.CreateDefault(true),
            PromptBinding.CreateDefault<string?>(null),
            PromptBinding.CreateDefault<string?>(CommonAgentApplicators.AspireSkillName),
            PromptBinding.CreateDefault(AspireSkillsMode.Embedded),
            PromptBinding.CreateDefault(AgentConfigurationScope.Project),
            CancellationToken.None).DefaultTimeout();

        Assert.Equal(CliExitCodes.InvalidCommand, result.ExitCode);
        Assert.Empty(result.SelectedSkills);
        Assert.Equal(installFailureMessage, Assert.Single(interactionService.DisplayedErrors));
    }

    [Fact]
    public async Task PromptAndChainAsync_PreSelectsBundleDefaultsIncludingAspireify()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var interactionService = new TestInteractionService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interactionService;
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector { DetectedEnvironments = AgentEnvironment.CopilotCli };
        });
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AgentInitCommand>();

        // Chained agent init pre-selects every bundle-sourced skill, which is the semantic
        // `aspire init` relies on so the one-time wiring skill chains into the flow.
        var result = await command.PromptAndChainAsync(
            interactionService,
            CliExitCodes.Success,
            workspace.WorkspaceRoot,
            PromptBinding.CreateDefault(true),
            PromptBinding.CreateDefault<string?>(null),
            PromptBinding.CreateDefault<string?>(CommonAgentApplicators.AspireSkillName),
            PromptBinding.CreateDefault(AspireSkillsMode.Embedded),
            PromptBinding.CreateDefault(AgentConfigurationScope.Project),
            CancellationToken.None).DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Contains(result.SelectedSkills, static skill => skill.HasName(CommonAgentApplicators.AspireifySkillName));
        Assert.Contains(result.SelectedSkills, static skill => skill.HasName(FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName));
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), CommonAgentApplicators.AspireifySkillName);
        AssertSkillFileExists(workspace.WorkspaceRoot, Path.Combine(".agents", "skills"), FakeAspireSkillsInstaller.AspireProjectV2MigrationSkillName);
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithNoneSkills_SucceedsWithNoSkillsInstalled()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all --skills none");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);

        // No skills selected, so no skill files should be created
        var aspireSkillPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", CommonAgentApplicators.AspireSkillName);
        Assert.False(Directory.Exists(aspireSkillPath), $"Expected no aspire skill directory but found {aspireSkillPath}");
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_DoesNotConfigureMcpByDefault()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.False(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json")));
        Assert.False(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, "opencode.jsonc")));
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithMcpTrue_ConfiguresMcp()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all --mcp");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json")));
        Assert.True(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, "opencode.jsonc")));
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithMcpFalse_DoesNotConfigureMcp()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents all --mcp=false");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.False(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json")));
        Assert.False(File.Exists(Path.Combine(workspace.WorkspaceRoot.FullName, "opencode.jsonc")));
    }

    [Fact]
    public async Task AgentInitCommand_NonInteractive_WithAgentNone_DoesNotInstallAnySkills()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        using var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents none --skills all");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);

        // With --agents none, no skill files should be installed even when --skills all is passed.
        var agentsDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills");
        Assert.False(Directory.Exists(agentsDir), $"Expected no agents/skills directory but found {agentsDir}");
    }

    private static void AssertSkillFileExists(DirectoryInfo workspaceRoot, string relativeSkillDirectory, string skillName)
    {
        var skillPath = Path.Combine(workspaceRoot.FullName, relativeSkillDirectory, skillName, "SKILL.md");
        Assert.True(File.Exists(skillPath), $"Expected skill file at {skillPath}");
    }

    [Fact]
    public async Task AgentInitCommand_BundledUsesTheManifestWithoutEnablingIndependentTools()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var bundle = await CreateBundleAsync(workspace.WorkspaceRoot,
            ("zeta-custom", "Additional bundle skill"), ("alpha-custom", "Another bundle skill"),
            (SkillDefinition.PlaywrightCli.Name, "Must not replace the independently selected tool"));
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AspireSkillsInstallerFactory = provider => new FakeAspireSkillsInstaller(
                provider.GetRequiredService<CliExecutionContext>(), AspireSkillsInstallResult.Installed(bundle));
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --workspace-root \"{workspace.WorkspaceRoot.FullName}\" --agents all --skills aspire --aspire-source embedded");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        foreach (var relativeDirectory in new[] { Path.Combine(".agents", "skills"), Path.Combine(".claude", "skills") })
        {
            var directory = Path.Combine(workspace.Path, relativeDirectory);
            Assert.Equal(["alpha-custom", "zeta-custom"], Directory.EnumerateDirectories(directory).Select(Path.GetFileName).Order());
            AssertSkillFileExists(workspace.WorkspaceRoot, relativeDirectory, "alpha-custom");
            AssertSkillFileExists(workspace.WorkspaceRoot, relativeDirectory, "zeta-custom");
        }
    }

    [Theory]
    [InlineData("apphost.cs", true)]
    [InlineData("apphost.mts", false)]
    [InlineData(null, false)]
    public async Task AgentInitCommand_DotnetInspect_RespectsWorkspaceLanguage(string? appHostFileName, bool expected)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        if (appHostFileName is not null)
        {
            await File.WriteAllTextAsync(Path.Combine(workspace.Path, appHostFileName), string.Empty);
        }
        string[] offeredSkills = [];
        var interactionService = new TestInteractionService
        {
            PromptForSelectionsCallback = (prompt, choices, _, _) =>
            {
                Assert.Equal(AgentCommandStrings.InitCommand_SelectSkills, prompt);
                offeredSkills = choices.Cast<string>().ToArray();
                return [];
            }
        };
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliHostEnvironmentFactory = _ => TestHelpers.CreateInteractiveHostEnvironment();
            options.InteractionServiceFactory = _ => interactionService;
        });
        services.AddSingleton<INpmRunner>(new FakeNpmRunner { IsAvailable = false });
        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        Assert.Equal(CliExitCodes.Success, await command.Parse(
            "agent init --agents copilot --scope project --mcp=false").InvokeAsync().DefaultTimeout());
        Assert.Equal(expected ? ["aspire", "playwright-cli", "dotnet-inspect"] : ["aspire", "playwright-cli"], offeredSkills);

        Assert.Equal(CliExitCodes.Success, await command.Parse(
            "agent init --non-interactive --agents copilot --scope project --skills all --aspire-source embedded").InvokeAsync().DefaultTimeout());
        Assert.Equal(expected, File.Exists(Path.Combine(workspace.Path, ".agents", "skills", SkillDefinition.DotnetInspect.Name, "SKILL.md")));
    }

    private static async Task<AspireSkillsBundle> CreateBundleAsync(DirectoryInfo workspaceRoot, params (string Name, string Description)[] skills)
    {
        var bundleDirectory = new DirectoryInfo(Path.Combine(workspaceRoot.FullName, $".test-aspire-skills-bundle-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(bundleDirectory.FullName);

        var manifestSkills = new List<SkillBundleSkill>();
        foreach (var (name, description) in skills)
        {
            var skillDirectory = Path.Combine(bundleDirectory.FullName, "skills", name);
            Directory.CreateDirectory(skillDirectory);
            var skillPath = Path.Combine(skillDirectory, "SKILL.md");
            await File.WriteAllTextAsync(skillPath, $$"""
                ---
                name: {{name}}
                description: "{{description}}"
                ---

                # {{name}}
                """);

            manifestSkills.Add(new SkillBundleSkill
            {
                Name = name,
                Description = description,
                Files =
                [
                    new SkillBundleFile
                    {
                        RelativePath = "SKILL.md",
                        Sha512 = ComputeSha512(skillPath)
                    }
                ]
            });
        }

        var manifest = new SkillBundleManifest
        {
            Version = AspireSkillsInstaller.Version,
            Supports = new SkillBundleSupports
            {
                AspireCli = ">=0.0.0 <999.0.0",
                AspireSdk = ">=0.0.0 <999.0.0"
            },
            Skills = [.. manifestSkills]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, AspireSkillsJsonSerializerContext.Default.SkillBundleManifest);
        var manifestPath = Path.Combine(bundleDirectory.FullName, "skill-manifest.json");
        await File.WriteAllTextAsync(manifestPath, manifestJson);
        return await new AspireSkillsBundleProvider().LoadAsync(
            bundleDirectory,
            CancellationToken.None);
    }

    private static string ComputeSha512(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA512.HashData(stream)).ToLowerInvariant();
    }

    [Theory]
    [InlineData(nameof(AgentEnvironment.CopilotCli), "GitHub Copilot CLI")]
    [InlineData(nameof(AgentEnvironment.CopilotApp), "GitHub Copilot App")]
    public async Task AgentInitCommand_GlobalScope_InstallsTelemetryHook_ForSelectedDetectedEnvironment(string environmentName, string displayName)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var interactionService = new TestInteractionService();
        var environment = Enum.Parse<AgentEnvironment>(environmentName);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory);
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector { DetectedEnvironments = environment };
            options.InteractionServiceFactory = _ => interactionService;
        });

        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents copilot --scope global --skills aspire");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        var hookFile = Path.Combine(homeDirectory.FullName, ".copilot", "hooks", "aspire-telemetry.json");
        Assert.True(File.Exists(hookFile), $"Expected telemetry hook at {hookFile}");
        Assert.Contains(
            interactionService.DisplayedMessages,
            message => message.Message.Contains(displayName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AgentInitCommand_DoesNotFail_WhenTelemetryHookConfigurationThrows()
    {
        // Hook installation is best-effort transparency tooling. A non-IO failure such as a missing
        // embedded hook script (InvalidOperationException from the installer) must not abort `agent init`.
        const string failureMessage = "simulated hook configuration failure";
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var interactionService = new TestInteractionService();
        var subtleMessages = new List<string>();
        interactionService.DisplaySubtleMessageCallback = subtleMessages.Add;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliExecutionContextFactory = _ => CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory);
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector { DetectedEnvironments = AgentEnvironment.CopilotCli };
            options.InteractionServiceFactory = _ => interactionService;
            options.TelemetryHookConfiguratorFactory = _ => new TestTelemetryHookConfigurator { Exception = new InvalidOperationException(failureMessage) };
        });

        using var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"agent init --workspace-root {workspace.WorkspaceRoot.FullName} --agents copilot --scope global --skills aspire");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(CliExitCodes.Success, exitCode);
        Assert.Contains(failureMessage, subtleMessages);
    }

    private static CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory, DirectoryInfo homeDirectory)
    {
        return TestExecutionContextHelper.CreateExecutionContext(
            workingDirectory,
            homeDirectory: homeDirectory);
    }

    [Theory]
    [InlineData("copilot", "project", ".github/copilot/settings.json")]
    [InlineData("copilot", "global", ".copilot/settings.json")]
    [InlineData("claude", "project", ".claude/settings.json")]
    [InlineData("claude", "global", ".claude/settings.json")]
    [InlineData("opencode", "project", "opencode.jsonc")]
    [InlineData("opencode", "global", ".config/opencode/opencode.jsonc")]
    public async Task Native_RegistersSelectedUndetectedClientsWithoutBundleOrMcp(string client, string scope, string relativePath)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var messages = new List<string>();
        var interaction = new TestInteractionService
        {
            DisplaySubtleMessageCallback = messages.Add,
            PromptForSelectionCallback = (_, _, _, _) => throw new InvalidOperationException("Explicit source and scope must not prompt.")
        };
        var services = context.CreateServiceCollection(outputHelper, options =>
        {
            options.CliHostEnvironmentFactory = _ => TestHelpers.CreateInteractiveHostEnvironment();
            options.InteractionServiceFactory = _ => interaction;
            options.AspireSkillsInstallerFactory = _ => new FakeAspireSkillsInstaller(context.ExecutionContext,
                AspireSkillsInstallResult.Failed("Native source must not acquire a bundle."));
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents {client} --scope {scope} --skills aspire --aspire-source native");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var root = scope == "project" ? context.Project : context.Home;
        var otherRoot = scope == "project" ? context.Home : context.Project;
        var expectedPath = Path.Combine(root.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal([expectedPath], Directory.EnumerateFiles(root.FullName, "*", SearchOption.AllDirectories));
        Assert.Empty(otherRoot.EnumerateFileSystemInfos());
        var config = JsonNode.Parse(await File.ReadAllTextAsync(expectedPath))!.AsObject();
        Assert.Equal(client == "opencode" ? ["skills"] : new[] { "enabledPlugins", "extraKnownMarketplaces" }, config.Select(property => property.Key).Order());
        Assert.Empty(messages);
        Assert.Collection(interaction.DisplayedMessages,
            message =>
            {
                Assert.Equal(KnownEmojis.Warning, message.Emoji);
                Assert.Equal(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_PluginRiskWarning,
                    AspireSkillsConfiguration.RepositoryUrl), message.Message);
            },
            message =>
            {
                Assert.Equal(KnownEmojis.Robot, message.Emoji);
                Assert.Equal(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_RegisteredTarget,
                    context.Clients.Single(value => value.Id == client).DisplayName,
                    scope == "project" ? AgentCommandStrings.InitCommand_ProjectScopeLabel : AgentCommandStrings.InitCommand_GlobalScopeLabel,
                    scope == "project" ? relativePath : $"~/{relativePath}"), message.Message);
            },
            message =>
            {
                Assert.Equal(KnownEmojis.Information, message.Emoji);
                Assert.Equal(AgentCommandStrings.InitCommand_ClientAcquisitionNotice, message.Message);
            });
        Assert.Empty(interaction.DisplayedErrors);
    }

    [Fact]
    public async Task Native_OpenCodeSkillsAndMcpUseIndependentRepresentations()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var path = Path.Combine(context.Project.FullName, "opencode.json");
        var original = JsonNode.Parse(
            """{"skills":{"paths":["./team-skills"]},"mcp":{"servers":{"aspire":{"type":"local","command":["pinned-aspire"],"disabled":true}}}}""")!;
        await File.WriteAllTextAsync(path, original.ToJsonString());
        using var provider = context.CreateServiceCollection(outputHelper).BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            "agent init --agents opencode --scope project --skills aspire --aspire-source native --mcp");

        Assert.Equal(CliExitCodes.Success, await result.InvokeAsync().DefaultTimeout());
        var configured = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        Assert.True(JsonNode.DeepEquals(original["mcp"], configured["mcp"]));
        Assert.Equal(["./team-skills"], configured["skills"]!["paths"]!.AsArray().Select(value => (string)value!));
        Assert.Equal(["https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/"],
            configured["skills"]!["urls"]!.AsArray().Select(value => (string)value!));
        Assert.Equal([path], context.Project.EnumerateFiles().Select(file => file.FullName));
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData(nameof(AgentConfigurationScope.Project))]
    [InlineData(nameof(AgentConfigurationScope.Global))]
    public async Task Mcp_InvalidOpenCodeNativeContainer_DoesNotStopIndependentClients(string scopeName)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var scope = Enum.Parse<AgentConfigurationScope>(scopeName);
        var path = context.OpenCode.GetMcpConfigPath(scope, context.Project);
        const string original = """{"mcp":{"servers":null}}""";
        await AgentConfigurationTestContext.WriteAsync(path, original);
        var interaction = new TestInteractionService();
        var hooks = new TestTelemetryHookConfigurator();
        var services = context.CreateServiceCollection(outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
            options.AgentEnvironmentDetectorFactory = _ => new TestAgentEnvironmentDetector { DetectedEnvironments = AgentEnvironment.CopilotCli };
            options.TelemetryHookConfiguratorFactory = _ => hooks;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents opencode,copilot --scope {scope} --skills none --mcp");

        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Equal(original, await File.ReadAllTextAsync(path));
        Assert.True(File.Exists(context.Copilot.GetMcpConfigPath(scope, context.Project)));
        Assert.Equal(scope is AgentConfigurationScope.Global ? AgentEnvironment.CopilotCli : AgentEnvironment.None, hooks.ConfiguredEnvironments);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture,
            AgentCommandStrings.McpServerContainerMustBeJsonObject, "mcp.servers", path), Assert.Single(interaction.DisplayedErrors));
    }

    [Theory]
    [InlineData("copilot", "{broken")]
    [InlineData("claude", "{broken")]
    [InlineData("opencode", """{"skills":[true],"mcp":{"servers":{}}}""")]
    public async Task Native_FailedRegistration_StillConfiguresIndependentlyRequestedMcp(string clientId, string malformed)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var client = context.Clients.Single(client => client.Id == clientId);
        var integrationPath = client.GetIntegrationConfigPath(AgentConfigurationScope.Project, context.Project);
        await AgentConfigurationTestContext.WriteAsync(integrationPath, malformed);
        var interaction = new TestInteractionService();
        var services = context.CreateServiceCollection(outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => interaction;
        });
        using var provider = services.BuildServiceProvider();

        var result = provider.GetRequiredService<RootCommand>().Parse(
            $"agent init --agents {clientId} --scope project --skills aspire --aspire-source native --mcp");

        Assert.Equal(CliExitCodes.InvalidCommand, await result.InvokeAsync().DefaultTimeout());
        Assert.Single(interaction.DisplayedErrors);
        var config = JsonNode.Parse(await File.ReadAllTextAsync(
            client.GetMcpConfigPath(AgentConfigurationScope.Project, context.Project)))!;
        if (clientId == "opencode")
        {
            Assert.Equal([true], config["skills"]!.AsArray().Select(value => (bool)value!));
            var server = config["mcp"]!["servers"]!["aspire"]!;
            Assert.Equal(["aspire", "agent", "mcp"], server["command"]!.AsArray().Select(value => (string)value!));
            Assert.False((bool)server["disabled"]!);
            Assert.Equal(["servers"], config["mcp"]!.AsObject().Select(property => property.Key));
        }
        else
        {
            Assert.Equal(malformed, await File.ReadAllTextAsync(integrationPath));
            var server = config["mcpServers"]!["aspire"]!;
            Assert.Equal("aspire", (string?)server["command"]);
            Assert.Equal(["agent", "mcp"], server["args"]!.AsArray().Select(value => (string?)value));
        }
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

}
