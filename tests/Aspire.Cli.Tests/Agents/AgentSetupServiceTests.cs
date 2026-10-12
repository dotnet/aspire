// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Agents;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;
using Spectre.Console;

namespace Aspire.Cli.Tests.Agents;

public class AgentSetupServiceTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("none", "GitHub Copilot")]
    [InlineData("GitHub Copilot", "GitHub Copilot")]
    [InlineData("Claude Code", "Claude Code")]
    [InlineData("OpenCode", "OpenCode")]
    [InlineData("all", "GitHub Copilot,Claude Code,OpenCode")]
    public async Task AgentDefaults_UseDetectedGroupsOrCopilotFallback(string detectedAgents, string expectedAgents)
    {
        using var context = new AgentConfigurationTestContext(output, new TestAgentCliRunner
        {
            CopilotVersion = detectedAgents is "GitHub Copilot" or "all" ? SemVersion.Parse("1.0.0", SemVersionStyles.Strict) : null,
            ClaudeCodeVersion = detectedAgents is "Claude Code" or "all" ? SemVersion.Parse("2.1.0", SemVersionStyles.Strict) : null,
            OpenCodeVersion = detectedAgents is "OpenCode" or "all" ? SemVersion.Parse("2.0.0", SemVersionStyles.Strict) : null
        });
        var discovery = await context.Setup.ScanAsync(context.Project, context.Project, CancellationToken.None).DefaultTimeout();

        var selected = await context.Setup.SelectAgentsAsync(PromptBinding.CreateDefault<string?>(null),
            discovery, interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.NotNull(selected);
        Assert.Equal(expectedAgents.Split(','), selected.Select(agent => agent.DisplayName));
        Assert.Equal(detectedAgents == "none" ? [] : expectedAgents.Split(','), discovery.Environments.Select(agent => agent.DisplayName));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    [Fact]
    public async Task AgentDefaults_IncludeDetectedAgentsWithoutRequiringMatchingPaths()
    {
        using var context = new AgentConfigurationTestContext(output, new TestAgentCliRunner
        {
            ClaudeCodeVersion = SemVersion.Parse("2.1.0", SemVersionStyles.Strict)
        });
        var discovery = await context.Setup.ScanAsync(context.Project, context.Project, CancellationToken.None).DefaultTimeout();

        var selected = await context.Setup.SelectAgentsAsync(PromptBinding.CreateDefault<string?>(null),
            discovery, interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.NotNull(selected);
        Assert.Equal(["Claude Code"], selected.Select(agent => agent.DisplayName));
        Assert.Equal([context.ClaudeCode], discovery.Environments);
        Assert.Equal(AgentEnvironment.ClaudeCode, Assert.Single(discovery.Detections).Client);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Fact]
    public async Task AgentDefaults_SharedNativeTargetsDoNotSelectUndetectedAgents()
    {
        using var context = new AgentConfigurationTestContext(output);
        context.SetVariable("OPENCODE_CONFIG", Path.Combine(context.CopilotDirectory, "mcp-config.json"));
        var discovery = await context.Setup.ScanAsync(context.Project, context.Project, CancellationToken.None).DefaultTimeout();

        var selected = await context.Setup.SelectAgentsAsync(PromptBinding.CreateDefault<string?>(null),
            discovery, interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.NotNull(selected);
        Assert.Equal(["GitHub Copilot"], selected.Select(agent => agent.DisplayName));
        Assert.Empty(discovery.Detections);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData("Project")]
    [InlineData("Global")]
    public void ScopeDescriptions_ShowOnlySelectedScopedAssetLocations(string scope)
    {
        using var context = new AgentConfigurationTestContext(output);
        var selectedScope = Enum.Parse<AgentConfigurationScope>(scope);
        var request = context.ManagedRequest(selectedScope, context.Environments, dotnetInspect: false);
        var prefix = selectedScope is AgentConfigurationScope.Global ? "~" : string.Empty;
        var label = selectedScope is AgentConfigurationScope.Project ? AgentCommandStrings.InitCommand_ProjectScope : AgentCommandStrings.InitCommand_GlobalScope;
        var expectedPaths = new[]
        {
            Path.Combine(prefix, ".agents", "skills", "playwright-cli"),
            Path.Combine(prefix, ".claude", "skills", "playwright-cli")
        };

        Assert.Equal($"{label} [dim]({string.Join(", ", expectedPaths)})[/]", context.Setup.DescribeScope(request));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Fact]
    public void ScopeDescriptions_ReportInvalidLocations()
    {
        using var context = new AgentConfigurationTestContext(output);
        context.SetVariable("COPILOT_HOME", "\0");
        var request = context.Request(AgentConfigurationScope.Global, [context.Copilot]);
        var error = Assert.Throws<ArgumentException>(() => AgentPath.Resolve(Assert.Single(context.Copilot.GetTargets(request)).Path));
        var message = string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.Configuration_ReadWriteFailed, error.Message);

        Assert.Equal($"{AgentCommandStrings.InitCommand_GlobalScope} [dim]({message.EscapeMarkup()})[/]", context.Setup.DescribeScope(request));
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Fact]
    public async Task HeadlessWithoutPluginConsent_LeavesTheOfferPending()
    {
        using var context = new AgentConfigurationTestContext(output);

        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false),
            PromptBinding.CreateDefault<string?>(null), interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.Interaction.ShownStatuses);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(await context.Configuration.GetGlobalConfigurationAsync(throwOnError: true).DefaultTimeout());
    }

    [Theory]
    [InlineData("--aspire-skills n")]
    [InlineData("--aspire-skills=false")]
    [InlineData("--agents none")]
    [InlineData("--aspire-skills y --agents NONE")]
    public async Task ExplicitDecline_CompletesTheOfferWithoutAgentWrites(string arguments)
    {
        using var context = new AgentConfigurationTestContext(output);
        var bindings = CreateBindings(arguments);

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Equal([".aspire"], context.Home.EnumerateDirectories().Select(directory => directory.Name));
        Assert.Equal("true", (await context.Configuration.GetGlobalConfigurationAsync(throwOnError: true).DefaultTimeout())[CliConfigNames.AgentSetupCompleted]);
    }

    [Fact]
    public async Task InteractiveDecline_IsNotOfferedAgain()
    {
        using var context = new AgentConfigurationTestContext(output);
        context.Interaction.SetupBooleanResponse(false);

        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false),
            PromptBinding.CreateDefault<string?>(null), interactive: true, CancellationToken.None).DefaultTimeout();
        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false),
            PromptBinding.CreateDefault<string?>(null), interactive: true, CancellationToken.None).DefaultTimeout();

        var prompt = Assert.Single(context.Interaction.BooleanPromptCalls);
        Assert.Equal(AgentCommandStrings.SetupCommand_PluginOffer, prompt.PromptText);
        Assert.False(prompt.DefaultValue);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Equal([".aspire"], context.Home.EnumerateDirectories().Select(directory => directory.Name));
    }

    [Fact]
    public async Task InteractiveConsent_ShowsGuidanceBeforeChoosingAnUndetectedAgent()
    {
        using var context = new AgentConfigurationTestContext(output);
        var events = new List<string>();
        context.Interaction.DisplayMessageCallback = (_, message, _) => events.Add(message);
        context.Interaction.DisplaySubtleMessageCallback = events.Add;
        context.Interaction.ConfirmCallback = (prompt, _) =>
        {
            events.Add(prompt);
            return true;
        };
        context.Interaction.PromptForSelectionsCallback = (_, _, _, _) =>
        {
            events.Add("selection");
            return [AgentClient.Copilot];
        };

        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false),
            PromptBinding.CreateDefault<string?>(null), interactive: true, CancellationToken.None).DefaultTimeout();

        var alternative = string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_DirectSkillsAlternative,
            AspireSkillsPluginConfiguration.RepositoryUrl);
        Assert.True(events.IndexOf(AgentCommandStrings.InitCommand_PluginRiskWarning) < events.IndexOf(AgentCommandStrings.SetupCommand_PluginOffer));
        Assert.True(events.IndexOf(alternative) < events.IndexOf(AgentCommandStrings.SetupCommand_PluginOffer));
        Assert.True(events.IndexOf(AgentCommandStrings.SetupCommand_PluginOffer) < events.IndexOf("selection"));
        Assert.True(File.Exists(Path.Combine(context.CopilotDirectory, "settings.json")));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    [Fact]
    public async Task UnattendedConsent_RegistersOnlyDetectedAgents()
    {
        using var context = new AgentConfigurationTestContext(output, new TestAgentCliRunner
        {
            CopilotVersion = SemVersion.Parse("1.0.0", SemVersionStyles.Strict)
        });
        await AgentConfigurationTestContext.WriteAsync(Path.Combine(context.Project.FullName, ".claude", "settings.json"), "{broken");
        await AgentConfigurationTestContext.WriteAsync(Path.Combine(context.Project.FullName, "opencode.json"), "{broken");
        var bindings = CreateBindings("--aspire-skills y");

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Equal(
            new[]
            {
                Path.Combine(".aspire", "settings.global.json"),
                Path.Combine(".copilot", "settings.json")
            }.Order(StringComparer.Ordinal),
            context.Home.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(context.Home.FullName, file.FullName)).Order(StringComparer.Ordinal));
        Assert.Equal("{broken", await File.ReadAllTextAsync(Path.Combine(context.Project.FullName, ".claude", "settings.json")).DefaultTimeout());
        Assert.Equal("{broken", await File.ReadAllTextAsync(Path.Combine(context.Project.FullName, "opencode.json")).DefaultTimeout());
        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    [Fact]
    public async Task UnattendedConsent_WithNoInstalledAgents_SelectsCopilot()
    {
        using var context = new AgentConfigurationTestContext(output);
        var bindings = CreateBindings("--aspire-skills y");

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Equal(
            new[]
            {
                Path.Combine(".aspire", "settings.global.json"),
                Path.Combine(".copilot", "settings.json")
            }.Order(StringComparer.Ordinal),
            context.Home.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(context.Home.FullName, file.FullName)).Order(StringComparer.Ordinal));
        Assert.Equal([AgentCommandStrings.InitCommand_ConfigurationComplete], context.Interaction.DisplayedSuccess);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
        Assert.Equal("true", (await context.Configuration.GetGlobalConfigurationAsync(throwOnError: true).DefaultTimeout())[CliConfigNames.AgentSetupCompleted]);
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    [InlineData("OpenCode")]
    public async Task UserOnlyRegistration_DoesNotReadProjectConfigurationOrSkills(string client)
    {
        using var context = new AgentConfigurationTestContext(output);
        string[] projectPaths =
        [
            Path.Combine(context.Project.FullName, ".github", "copilot", "settings.json"),
            Path.Combine(context.Project.FullName, ".claude", "settings.json"),
            Path.Combine(context.Project.FullName, "opencode.json")
        ];
        foreach (var path in projectPaths)
        {
            await AgentConfigurationTestContext.WriteAsync(path, "{broken").DefaultTimeout();
        }
        var skill = Path.Combine(context.Project.FullName, ".agents", "skills", "aspire", "SKILL.md");
        await AgentConfigurationTestContext.WriteAsync(skill, "Project-specific instructions.").DefaultTimeout();
        var bindings = CreateBindings($"--aspire-skills y --agents \"{client}\"");

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.Interaction.DisplayedErrors);
        Assert.Equal([AgentCommandStrings.InitCommand_ConfigurationComplete], context.Interaction.DisplayedSuccess);
        Assert.Equal([KnownEmojis.Warning, KnownEmojis.CheckMarkButton],
            context.Interaction.DisplayedMessages.Select(message => message.Emoji));
        foreach (var path in projectPaths)
        {
            Assert.Equal("{broken", await File.ReadAllTextAsync(path).DefaultTimeout());
        }
        Assert.Equal("Project-specific instructions.", await File.ReadAllTextAsync(skill).DefaultTimeout());
        Assert.Equal(2, context.Home.EnumerateFiles("*", SearchOption.AllDirectories).Count());
        Assert.Empty(context.SkillInstaller.Requests);
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    [Fact]
    public async Task UserOnlyRegistration_WarnsAboutUserSkillsWithoutChangingThem()
    {
        using var context = new AgentConfigurationTestContext(output);
        var skill = Path.Combine(context.CopilotDirectory, "skills", "aspire", "SKILL.md");
        await AgentConfigurationTestContext.WriteAsync(skill, "Customized user instructions.").DefaultTimeout();
        var timestamp = File.GetLastWriteTimeUtc(skill);
        var bindings = CreateBindings("--aspire-skills y --agents \"GitHub Copilot\"");

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Single(context.Interaction.DisplayedMessages, message => message.Message ==
            string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_LocalSkillsConflict, skill));
        Assert.Equal("Customized user instructions.", await File.ReadAllTextAsync(skill).DefaultTimeout());
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(skill));
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
    }

    [Fact]
    public async Task CancellationBeforeConsent_LeavesTheOfferPending()
    {
        using var context = new AgentConfigurationTestContext(output);
        context.Interaction.SetupCancellationResponse();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Setup.OfferUserPluginSetupAsync(
            PromptBinding.CreateDefault(false), PromptBinding.CreateDefault<string?>(null),
            interactive: true, CancellationToken.None)).DefaultTimeout();

        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.CliRunner.Commands);
    }

    [Fact]
    public async Task PolicyBlockedRegistration_IsReportedAndDoesNotRepeatTheOffer()
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = Path.Combine(context.CopilotDirectory, "settings.json");
        const string existing = """{"strictKnownMarketplaces":[]}""";
        await AgentConfigurationTestContext.WriteAsync(path, existing).DefaultTimeout();
        var bindings = CreateBindings("--aspire-skills y --agents \"GitHub Copilot\"");

        await context.Setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();
        var commands = context.CliRunner.Commands.ToArray();
        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false),
            PromptBinding.CreateDefault<string?>(null), interactive: true, CancellationToken.None).DefaultTimeout();

        Assert.Single(context.Interaction.DisplayedErrors);
        Assert.Empty(context.Interaction.DisplayedSuccess);
        Assert.Single(context.Interaction.DisplayedMessages, message => message.Message == AgentCommandStrings.SetupCommand_PluginRetry);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Equal(commands, context.CliRunner.Commands);
        Assert.Equal("true", (await context.Configuration.GetGlobalConfigurationAsync(throwOnError: true).DefaultTimeout())[CliConfigNames.AgentSetupCompleted]);
    }

    [Fact]
    public async Task CompletionPersistenceFailure_IsExplicitlyReported()
    {
        using var context = new AgentConfigurationTestContext(output);
        var configuration = new TestConfigurationService
        {
            OnSetConfiguration = (_, _, _) => throw new IOException("Settings are read-only.")
        };
        var setup = new AgentSetupService(context.Environments, context.Writer, context.SkillInstaller, context.Interaction,
            context.ExecutionContext, context.Environment, configuration, NullLogger<AgentSetupService>.Instance);
        var bindings = CreateBindings("--aspire-skills n");

        await setup.OfferUserPluginSetupAsync(bindings.Plugin, bindings.Agents,
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Single(context.Interaction.DisplayedMessages, message => message.Message ==
            string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.SetupCommand_OfferStateWriteFailed, "Settings are read-only."));
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Fact]
    public async Task CompletionReadFailure_IsReportedWithoutPromptingOrWriting()
    {
        using var context = new AgentConfigurationTestContext(output);
        var configuration = new TestConfigurationService
        {
            OnGetGlobalConfiguration = () => throw new IOException("Settings could not be read."),
            OnSetConfiguration = (_, _, _) => Assert.Fail("Uncertain completion state must not be overwritten.")
        };
        var setup = new AgentSetupService(context.Environments, context.Writer, context.SkillInstaller, context.Interaction,
            context.ExecutionContext, context.Environment, configuration, NullLogger<AgentSetupService>.Instance);

        await setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false), PromptBinding.CreateDefault<string?>(null),
            interactive: true, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Single(context.Interaction.DisplayedMessages, message => message.Message ==
            string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.SetupCommand_OfferStateReadFailed, "Settings could not be read."));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task InvalidGlobalCompletionFile_IsReportedWithoutPromptingOrOverwriting(string contents)
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = context.Configuration.GetSettingsFilePath(isGlobal: true);
        await AgentConfigurationTestContext.WriteAsync(path, contents).DefaultTimeout();

        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false), PromptBinding.CreateDefault<string?>(null),
            interactive: true, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Interaction.DisplayedSuccess);
        Assert.Equal(KnownEmojis.Warning, Assert.Single(context.Interaction.DisplayedMessages).Emoji);
        Assert.Equal(contents, await File.ReadAllTextAsync(path).DefaultTimeout());
    }

    [Fact]
    public async Task UnreadableGlobalCompletionFile_IsReportedWithoutPromptingOrWriting()
    {
        using var context = new AgentConfigurationTestContext(output);
        var path = context.Configuration.GetSettingsFilePath(isGlobal: true);
        Directory.CreateDirectory(path);

        await context.Setup.OfferUserPluginSetupAsync(PromptBinding.CreateDefault(false), PromptBinding.CreateDefault<string?>(null),
            interactive: true, CancellationToken.None).DefaultTimeout();

        Assert.Empty(context.Interaction.BooleanPromptCalls);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Interaction.DisplayedSuccess);
        Assert.Equal(KnownEmojis.Warning, Assert.Single(context.Interaction.DisplayedMessages).Emoji);
        Assert.True(Directory.Exists(path));
        Assert.Empty(context.Home.EnumerateFiles("*", SearchOption.AllDirectories));
    }

    private static (PromptBinding<bool> Plugin, PromptBinding<string?> Agents) CreateBindings(string arguments)
    {
        var command = new Command("setup");
        var plugin = AgentSetupService.CreateAssetOption("--aspire-skills", string.Empty);
        var agents = AgentSetupService.CreateAgentsOption();
        command.Options.Add(plugin);
        command.Options.Add(agents);
        var parseResult = command.Parse(arguments);
        Assert.Empty(parseResult.Errors);

        return (AgentSetupService.CreateAssetBinding(parseResult, plugin, defaultValue: false), PromptBinding.Create(parseResult, agents));
    }
}
