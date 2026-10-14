// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Agents;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Microsoft.AspNetCore.InternalTesting;
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

    [Theory]
    [InlineData("bundled")]
    [InlineData("native")]
    [InlineData("none")]
    public async Task ExplicitSkillsSource_SkipsThePicker(string value)
    {
        var mode = Enum.Parse<AspireSkillsMode>(value, ignoreCase: true);
        using var context = new AgentConfigurationTestContext(output);
        context.Interaction.PromptForSelectionCallback = (_, _, _, _) => throw new InvalidOperationException("An explicit source must not prompt.");

        var command = new System.CommandLine.Command("init");
        command.Options.Add(AgentInitCommand.s_aspireSkillsOption);
        var parseResult = command.Parse($"--aspire-skills {mode}");
        var selected = await context.Setup.PromptForAspireSkillsAsync(
            PromptBinding.Create(parseResult, AgentInitCommand.s_aspireSkillsOption, AspireSkillsMode.Bundled),
            interactive: false, CancellationToken.None).DefaultTimeout();

        Assert.Equal(mode, selected);
        Assert.Empty(context.CliRunner.Commands);
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
    }
}
