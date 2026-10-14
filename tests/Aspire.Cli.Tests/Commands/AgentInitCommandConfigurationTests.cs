// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Agents.Playwright;
using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Agents;
using Aspire.Cli.Tests.TestServices;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;

namespace Aspire.Cli.Tests.Commands;

public class AgentInitCommandConfigurationTests(ITestOutputHelper output) : IDisposable
{
    private readonly AgentConfigurationTestContext _context = new(output);

    [Fact]
    public async Task NoAssets_DoesNotReadConfigOrInvokeAnyInstaller()
    {
        await AgentConfigurationTestContext.WriteAsync(Path.Combine(_context.CopilotDirectory, "settings.json"), "{broken").DefaultTimeout();

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [_context.Copilot], skills: false, detections: [new(AgentEnvironment.CopilotCli, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.Empty(result.Targets);
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_context.Project.FullName));
    }

    [Fact]
    public async Task NoClients_IsANoOpEvenWhenAllAssetsAreSelected()
    {

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [], mcp: true, playwright: true, dotnetInspect: true,
                detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.Empty(result.Targets);
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_context.Project.FullName));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_context.Home.FullName));
    }

    [Theory]
    [InlineData("copilot", "Project")]
    [InlineData("copilot", "Global")]
    [InlineData("claude", "Project")]
    [InlineData("claude", "Global")]
    [InlineData("opencode", "Project")]
    [InlineData("opencode", "Global")]
    public async Task NativeSkills_WithCopiedAspireSkills_StopBeforeAnyWritesOrAcquisition(string clientId, string scopeName)
    {
        var client = _context.Environments.Single(scanner => scanner.Client.Id == clientId);
        var scope = Enum.Parse<AgentConfigurationScope>(scopeName);
        var directory = AgentSkillInstaller.GetSkillBaseDirectory(client, scope, _context.Project, _context.ExecutionContext, _context.Environment);
        var skillPath = Path.Combine(directory, "aspireify", "SKILL.md");
        const string original = "Customized Aspire skill.";
        await AgentConfigurationTestContext.WriteAsync(skillPath, original);
        var before = Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories);

        var result = await _context.ConfigureAsync(
            _context.Request(scope, [client], mcp: true, playwright: true, dotnetInspect: true),
            CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        var blocked = Assert.Single(result.Targets);
        Assert.Equal(AgentConfigurationStatus.Blocked, blocked.Status);
        Assert.Equal(AgentAssetKind.AspireSkills, blocked.Asset);
        Assert.Contains(skillPath, blocked.Message);
        Assert.Equal(original, await File.ReadAllTextAsync(skillPath));
        Assert.Equal(before, Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories));
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
    }

    [Theory]
    [InlineData("copilot", "Project")]
    [InlineData("copilot", "Global")]
    [InlineData("claude", "Project")]
    [InlineData("claude", "Global")]
    [InlineData("opencode", "Project")]
    [InlineData("opencode", "Global")]
    public async Task BundledSkills_WithNativeRegistrationInEitherScope_StopBeforeAnyWritesOrAcquisition(string clientId, string nativeScopeName)
    {
        var client = _context.Environments.Single(scanner => scanner.Client.Id == clientId);
        var nativeScope = Enum.Parse<AgentConfigurationScope>(nativeScopeName);
        var native = Assert.Single(await _context.ConfigureNativeAsync(_context.Request(nativeScope, [client])));
        Assert.Equal(AgentConfigurationStatus.Configured, native.Status);
        var original = await File.ReadAllTextAsync(native.TargetPath);
        var before = Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories);
        var scope = nativeScope is AgentConfigurationScope.Project ? AgentConfigurationScope.Global : AgentConfigurationScope.Project;
        var request = _context.Request(scope, [client]) with
        {
            Assets = new(true, true, true, AspireSkillsMode.Bundled)
        };

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        var blocked = Assert.Single(result.Targets);
        Assert.Equal(AgentConfigurationStatus.Blocked, blocked.Status);
        Assert.Equal(AgentAssetKind.BundledAspireSkills, blocked.Asset);
        Assert.Equal(original, await File.ReadAllTextAsync(native.TargetPath));
        Assert.Equal(before, Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories));
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
    }

    [Theory]
    [InlineData("Project", false)]
    [InlineData("Project", true)]
    [InlineData("Global", false)]
    [InlineData("Global", true)]
    public async Task BundledSkills_OpenCodeCatalogInLowerPrecedenceJson_StopsBeforeAnyWrites(string scopeName, bool v2)
    {
        var scope = Enum.Parse<AgentConfigurationScope>(scopeName);
        var directory = scope is AgentConfigurationScope.Project ? _context.Project.FullName : _context.OpenCodeDirectory;
        var json = Path.Combine(directory, "opencode.json");
        var jsonc = Path.Combine(directory, "opencode.jsonc");
        var catalog = v2
            ? """{"skills":["https://raw.githubusercontent.com/microsoft/aspire-skills/v0.0.3/opencode/v2/"]}"""
            : """{"skills":{"urls":["https://raw.githubusercontent.com/microsoft/aspire-skills/v0.0.3/opencode/v1/"]}}""";
        const string higherPrecedence = """{"model":"preserved"}""";
        await AgentConfigurationTestContext.WriteAsync(json, catalog).DefaultTimeout();
        await AgentConfigurationTestContext.WriteAsync(jsonc, higherPrecedence).DefaultTimeout();
        var before = Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories);
        var request = _context.Request(scope, [_context.OpenCode]) with
        {
            Assets = new(true, true, true, AspireSkillsMode.Bundled)
        };

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        var blocked = Assert.Single(result.Targets);
        Assert.Equal(AgentConfigurationStatus.Blocked, blocked.Status);
        Assert.Equal(AgentAssetKind.BundledAspireSkills, blocked.Asset);
        Assert.Equal(json, blocked.TargetPath);
        Assert.Equal(catalog, await File.ReadAllTextAsync(json).DefaultTimeout());
        Assert.Equal(higherPrecedence, await File.ReadAllTextAsync(jsonc).DefaultTimeout());
        Assert.Equal(before, Directory.GetFiles(_context.Workspace.Path, "*", SearchOption.AllDirectories));
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
    }

    [Theory]
    [InlineData("copilot", true)]
    [InlineData("copilot", false)]
    [InlineData("claude", true)]
    [InlineData("claude", false)]
    public async Task BundledSkills_InspectInheritedPluginSettingsWithoutEnablingDisabledPlugins(string clientId, bool enabled)
    {
        var client = _context.Environments.Single(scanner => scanner.Client.Id == clientId);
        var path = Path.Combine(_context.Project.FullName, ".claude", "settings.local.json");
        var original = new JsonObject
        {
            ["enabledPlugins"] = new JsonObject { ["aspire@aspire-skills"] = enabled }
        }.ToJsonString();
        await AgentConfigurationTestContext.WriteAsync(path, original);
        var request = _context.Request(AgentConfigurationScope.Project, [client]) with
        {
            Assets = new(false, false, false, AspireSkillsMode.Bundled)
        };

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.Equal(enabled, result.HasErrors);
        Assert.Equal(original, await File.ReadAllTextAsync(path));
        if (enabled)
        {
            Assert.Equal(AgentConfigurationStatus.Blocked, Assert.Single(result.Targets).Status);
            Assert.Empty(_context.SkillInstaller.Requests);
        }
        else
        {
            Assert.Empty(result.Targets);
            Assert.Same(request, Assert.Single(_context.SkillInstaller.Requests));
        }
    }

    [Theory]
    [InlineData("""{"skills":{"urls":["https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/"]}}""")]
    [InlineData("""{"skills":["https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v2/"]}""")]
    public async Task BundledSkills_WithInlineOpenCodeCatalog_StopBeforeAnyWrites(string inline)
    {
        _context.SetVariable("OPENCODE_CONFIG_CONTENT", inline);
        var request = _context.Request(AgentConfigurationScope.Global, [_context.OpenCode]) with
        {
            Assets = new(true, true, true, AspireSkillsMode.Bundled)
        };

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        var blocked = Assert.Single(result.Targets);
        Assert.Equal(AgentConfigurationStatus.Blocked, blocked.Status);
        Assert.Equal("OPENCODE_CONFIG_CONTENT", blocked.TargetPath);
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        Assert.Equal(0, _context.HookInstaller.Calls);
    }

    [Theory]
    [InlineData("Project")]
    [InlineData("Global")]
    public async Task AllAssets_OnlyWriteTheSelectedScope(string scopeName)
    {
        var scope = Enum.Parse<AgentConfigurationScope>(scopeName);
        var request = _context.Request(scope, _context.Environments, mcp: true, playwright: true, dotnetInspect: true,
            detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)]);

        var result = await _context.CreateSetupService(_context.CreateManagedSkillInstaller())
            .ConfigureAsync(request, _context.Hooks, CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.NotEmpty(result.Targets);
        Assert.All(result.Targets, target => Assert.Equal(scope, target.Scope));
        var untouched = scope is AgentConfigurationScope.Project ? _context.Home : _context.Project;
        Assert.Empty(untouched.EnumerateFileSystemInfos());
        Assert.Equal(scope is AgentConfigurationScope.Global ? 1 : 0, _context.HookInstaller.Calls);
        Assert.Equal(scope is AgentConfigurationScope.Global ? 2 : 0, result.Targets.Count(target => target.Asset is AgentAssetKind.TelemetryHooks));
        Assert.Equal(1, _context.Playwright.InstallSkillsCallCount);
    }

    [Fact]
    public async Task EditorHint_DoesNotCreateCliHooksOrEditorSettings()
    {
        var request = _context.Request(AgentConfigurationScope.Global, [_context.Copilot],
            detections: [new(AgentEnvironment.VsCode, null)]);

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        var target = Assert.Single(result.Targets);
        Assert.Equal(AgentAssetKind.AspireSkills, target.Asset);
        Assert.Equal(Path.Combine(_context.CopilotDirectory, "settings.json"), target.TargetPath);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        Assert.Equal([".copilot"], _context.Home.EnumerateDirectories().Select(directory => directory.Name));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ToolSkills_UseOneManagedInvocationAndConfigureOnlyDetectedHooks(bool playwright, bool dotnetInspect)
    {
        var request = _context.Request(AgentConfigurationScope.Global, [_context.Copilot, _context.ClaudeCode],
            skills: false, playwright: playwright, dotnetInspect: dotnetInspect,
            detections: [new(AgentEnvironment.CopilotCli, null)]);
        _context.SkillInstaller.Results =
        [
            new(playwright ? AgentAssetKind.Playwright : AgentAssetKind.DotnetInspect, request.Environments,
                Path.Combine(_context.Home.FullName, "managed-skill"), AgentConfigurationScope.Global, AgentConfigurationStatus.Configured, "test")
        ];

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.Same(request, Assert.Single(_context.SkillInstaller.Requests));
        Assert.Equal(2, result.Targets.Count);
        Assert.Equal(_context.SkillInstaller.Results[0], result.Targets[1]);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal([_context.Copilot], hook.Environments);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        Assert.Equal(AgentConfigurationScope.Global, hook.Scope);
        Assert.Equal(1, _context.HookInstaller.Calls);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_context.Project.FullName));
        Assert.False(File.Exists(Path.Combine(_context.CopilotDirectory, "settings.json")));
        Assert.False(Directory.Exists(_context.ClaudeDirectory));
    }

    [Fact]
    public async Task NativeSkills_AreOfflineAndInstallOnlyOneSetOfEmbeddedHooks()
    {
        var request = _context.Request(AgentConfigurationScope.Global,
            [_context.Copilot, _context.ClaudeCode, _context.OpenCode],
            detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)]);

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(1, _context.HookInstaller.Calls);
        Assert.Equal(2, result.Targets.Count(target => target.Asset is AgentAssetKind.TelemetryHooks));
        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.TelemetryHooks),
            target => Assert.Equal(AgentConfigurationScope.Global, target.Scope));
        Assert.Equal(request.Environments.OrderBy(client => client.DisplayName), result.RegisteredEnvironments.OrderBy(client => client.DisplayName));
        Assert.False(File.Exists(Path.Combine(_context.CopilotDirectory, "config.json")));
        Assert.False(Directory.Exists(Path.Combine(_context.CopilotDirectory, "installed-plugins")));
    }

    [Fact]
    public async Task NativeFailures_DoNotPreventIndependentTargetsAndMalformedPolicyStillBlocksHooks()
    {
        var path = Path.Combine(_context.CopilotDirectory, "settings.json");
        await AgentConfigurationTestContext.WriteAsync(path, "{broken").DefaultTimeout();

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [_context.Copilot, _context.OpenCode],
                detections: [new(AgentEnvironment.CopilotCli, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        Assert.True(result.HasWarnings);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills && target.Environments.Contains(_context.Copilot)),
            target => Assert.Equal(AgentConfigurationStatus.Blocked, target.Status));
        Assert.All(result.Targets.Where(target => target.Environments.Contains(_context.OpenCode)),
            target => Assert.Equal(AgentConfigurationStatus.Configured, target.Status));
        Assert.Equal(AgentConfigurationStatus.Blocked, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).Status);
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    public async Task NativeRegistrationBlocked_DoesNotPreventDetectedClientHooks(string clientName)
    {
        var client = _context.Environments.Single(client => client.DisplayName == clientName);
        var detectedClient = clientName == "GitHub Copilot" ? AgentEnvironment.CopilotCli : AgentEnvironment.ClaudeCode;
        var directory = client == _context.ClaudeCode ? _context.ClaudeDirectory : _context.CopilotDirectory;
        var path = Path.Combine(directory, "settings.json");
        await AgentConfigurationTestContext.WriteAsync(path,
            """{"extraKnownMarketplaces":{"aspire-skills":{"source":{"source":"github","repo":"example/custom-skills"}}}}""").DefaultTimeout();

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [client], detections: [new(detectedClient, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Blocked, target.Status));
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        Assert.Equal([client], hook.Environments);
        Assert.Equal(1, _context.HookInstaller.Calls);
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(path).DefaultTimeout())!;
        Assert.Equal("example/custom-skills", settings["extraKnownMarketplaces"]!["aspire-skills"]!["source"]!["repo"]!.GetValue<string>());
    }

    [Fact]
    public async Task HookInstallationFailures_AreExplicitAndAdvisory()
    {
        _context.HookInstaller.Error = new IOException("Hook directory is locked.");

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [_context.Copilot, _context.ClaudeCode],
                detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.True(result.HasWarnings);
        Assert.Equal(1, _context.HookInstaller.Calls);
        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Configured, target.Status));
        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.TelemetryHooks), target =>
        {
            Assert.Equal(AgentConfigurationStatus.Failed, target.Status);
            Assert.NotEmpty(target.Message!);
        });
        var claudeSettings = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(_context.ClaudeDirectory, "settings.json")).DefaultTimeout())!;
        Assert.True((bool)claudeSettings["enabledPlugins"]!["aspire@aspire-skills"]!);
        Assert.Null(claudeSettings["hooks"]);
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    public async Task McpSetup_AddsUserHookForDetectedClientsWithoutAspireSkills(string clientName)
    {
        var client = _context.Environments.Single(client => client.DisplayName == clientName);
        var detectedClient = clientName == "GitHub Copilot" ? AgentEnvironment.CopilotCli : AgentEnvironment.ClaudeCode;

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [client], skills: false, mcp: true, detections: [new(detectedClient, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.Empty(result.RegisteredEnvironments);
        Assert.Equal(1, _context.HookInstaller.Calls);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        Assert.Equal(AgentConfigurationScope.Global, hook.Scope);
        Assert.Empty(_context.SkillInstaller.Requests);
    }

    [Fact]
    public async Task AlreadyConfiguredNativeSources_DoNotPreventAMissingDetectedClientHook()
    {
        var request = _context.Request(AgentConfigurationScope.Global, [_context.Copilot],
            detections: [new(AgentEnvironment.CopilotCli, null)]);
        await _context.ConfigureNativeAsync(request).DefaultTimeout();

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        Assert.Equal(AgentConfigurationStatus.Configured, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).Status);
    }

    [Fact]
    public async Task ClaudeSettingsAndHook_AreMergedAndRemainByteStableOnRepeat()
    {
        var path = Path.Combine(_context.ClaudeDirectory, "settings.json");
        await AgentConfigurationTestContext.WriteAsync(path, """{"model":"preserved","hooks":{"PreToolUse":[]}}""").DefaultTimeout();
        var request = _context.Request(AgentConfigurationScope.Global, [_context.ClaudeCode],
            detections: [new(AgentEnvironment.ClaudeCode, null)]);

        var first = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();
        var bytes = await File.ReadAllBytesAsync(path).DefaultTimeout();
        File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(path);
        var second = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.False(first.HasErrors);
        Assert.All(second.Targets, result => Assert.Equal(AgentConfigurationStatus.Unchanged, result.Status));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path).DefaultTimeout());
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
        var root = JsonNode.Parse(bytes)!.AsObject();
        var hook = root["hooks"]!["PostToolUse"]![0]!["hooks"]![0]!.AsObject();
        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        Assert.Equal(command, (string?)hook["command"]);
        Assert.Equal(args, hook["args"]!.AsArray().Select(value => (string)value!));
        // The real invocation is checked above; the snapshot excludes host/assembly paths
        // that differ between managed test runners and a native CLI executable.
        hook["command"] = "[aspire-executable]";
        hook["args"] = new JsonArray(AgentTelemetryProtocol.AgentCommandName,
            AgentTelemetryProtocol.TelemetryCommandName, AgentTelemetryProtocol.HookOptionName);
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await Verify(json, "json")
            .UseFileName($"AgentInitCommandConfigurationTests.ClaudeSettingsAndHook.{(OperatingSystem.IsWindows() ? "Windows" : "Unix")}");
    }

    [Fact]
    public async Task DetectedButUnselectedClients_DoNotReceivePrivateConfigurationOrHooks()
    {
        var request = _context.Request(AgentConfigurationScope.Global, [_context.OpenCode],
            detections: [new(AgentEnvironment.CopilotCli, SemVersion.Parse("1.0.0", SemVersionStyles.Strict)), new(AgentEnvironment.ClaudeCode, SemVersion.Parse("2.1.0", SemVersionStyles.Strict)), new(AgentEnvironment.VsCode, null)]);

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        var target = Assert.Single(result.Targets);
        Assert.Equal(AgentAssetKind.AspireSkills, target.Asset);
        Assert.Equal(AgentConfigurationStatus.Configured, target.Status);
        Assert.Equal([_context.OpenCode], target.Environments);
        Assert.Equal([_context.OpenCode], result.RegisteredEnvironments);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.False(Directory.Exists(_context.CopilotDirectory));
        Assert.False(Directory.Exists(_context.ClaudeDirectory));
        Assert.Equal([".config"], _context.Home.EnumerateDirectories().Select(directory => directory.Name));
        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        Assert.Equal(["opencode.jsonc"], Directory.EnumerateFiles(_context.OpenCodeDirectory).Select(Path.GetFileName));
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    public async Task SelectedButUndetectedClient_ReceivesNativeConfigurationWithoutHooks(string clientName)
    {
        var client = _context.Environments.Single(client => client.DisplayName == clientName);

        var result = await _context.ConfigureAsync(_context.Request(AgentConfigurationScope.Global, [client]), CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.Single(result.Targets);
        Assert.All(result.Targets, target =>
        {
            Assert.Equal(AgentAssetKind.AspireSkills, target.Asset);
            Assert.Equal(AgentConfigurationStatus.Configured, target.Status);
            Assert.Equal([client], target.Environments);
        });
        Assert.Equal(0, _context.HookInstaller.Calls);
    }

    [Fact]
    public async Task CancellationFromHookInstallation_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        _context.HookInstaller.Error = new OperationCanceledException(cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _context.ConfigureAsync(
                _context.Request(AgentConfigurationScope.Global, [_context.Copilot], playwright: true, detections: [new(AgentEnvironment.CopilotCli, null)]),
                cancellation.Token)).DefaultTimeout();

        Assert.Empty(_context.SkillInstaller.Requests);
    }

    [Fact]
    public async Task UnreadableSkillLocations_BlockModeSelectionBeforeAnyWrites()
    {
        _context.SetVariable("COPILOT_HOME", "\0invalid");

        var result = await _context.ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [_context.Copilot, _context.OpenCode],
                detections: [new(AgentEnvironment.CopilotCli, null)]),
            CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        Assert.Equal(AgentConfigurationStatus.Failed, Assert.Single(result.Targets).Status);
        Assert.Empty(_context.SkillInstaller.Requests);
        Assert.Equal(0, _context.HookInstaller.Calls);
        Assert.Empty(_context.Home.EnumerateFileSystemInfos());
        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("[]")]
    [InlineData("""{"duplicate":1,"duplicate":2}""")]
    [InlineData("""{"nested":{"duplicate":1,"duplicate":2}}""")]
    [InlineData("""{"items":[{"duplicate":1,"duplicate":2}]}""")]
    [InlineData("""{"skills":[],"skills":{}}""")]
    public async Task MalformedOpenCodeInlineConfiguration_DoesNotAbortOtherNativeOrManagedRequests(string inline)
    {
        _context.SetVariable("OPENCODE_CONFIG_CONTENT", inline);
        _context.SkillInstaller.Results =
        [
            new(AgentAssetKind.DotnetInspect, [_context.OpenCode, _context.Copilot],
                Path.Combine(_context.Home.FullName, ".agents", "skills", "dotnet-inspect"),
                AgentConfigurationScope.Global, AgentConfigurationStatus.Configured, "test")
        ];
        var request = _context.Request(AgentConfigurationScope.Global, [_context.OpenCode, _context.Copilot], mcp: true, dotnetInspect: true);

        var result = await _context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.True(result.HasErrors);
        var openCode = result.Targets.Where(target => target.Environments.Contains(_context.OpenCode) &&
            target.Asset is AgentAssetKind.AspireSkills or AgentAssetKind.Mcp).ToArray();
        Assert.Equal(2, openCode.Length);
        Assert.All(openCode, target => Assert.Equal(AgentConfigurationStatus.Blocked, target.Status));
        var copilot = result.Targets.Where(target => target.Environments.Contains(_context.Copilot) &&
            target.Asset is AgentAssetKind.AspireSkills or AgentAssetKind.Mcp).ToArray();
        Assert.Equal(2, copilot.Length);
        Assert.All(copilot, target => Assert.Equal(AgentConfigurationStatus.Configured, target.Status));
        Assert.Same(request, Assert.Single(_context.SkillInstaller.Requests));
        Assert.Equal(_context.SkillInstaller.Results[0], Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.DotnetInspect));
        Assert.False(File.Exists(Path.Combine(_context.Project.FullName, "opencode.json")));
        Assert.False(Directory.Exists(_context.OpenCodeDirectory));
    }

    [Theory]
    [InlineData("~", "")]
    [InlineData("~/claude-work", "claude-work")]
    [InlineData(@"~\claude-work", "claude-work")]
    public async Task ClaudeHomeOverride_IsSharedByNativeRegistrationAndRealManagedSkills(string value, string relative)
    {
        _context.SetVariable("CLAUDE_CONFIG_DIR", value);
        var npm = new FakeNpmRunner();
        var playwright = new FakePlaywrightCliRunner();
        var playwrightInstaller = new PlaywrightCliInstaller(npm, new FakeNpmProvenanceChecker(), playwright,
            new TestInteractionService(), new ConfigurationBuilder().Build(), NullLogger<PlaywrightCliInstaller>.Instance);
        var managed = new AgentSkillInstaller(playwrightInstaller, new FakeAspireSkillsInstaller(_context.ExecutionContext),
            new TestLanguageDiscovery(), _context.ExecutionContext, _context.Environment, NullLogger<AgentSkillInstaller>.Instance);
        var expectedDirectory = relative.Length == 0 ? _context.Home.FullName : Path.Combine(_context.Home.FullName, relative);

        var result = await _context.CreateSetupService(managed).ConfigureAsync(
            _context.Request(AgentConfigurationScope.Global, [_context.ClaudeCode], dotnetInspect: true), _context.Hooks, CancellationToken.None).DefaultTimeout();

        Assert.False(result.HasErrors);
        Assert.All(result.Targets, target => Assert.Equal(AgentConfigurationStatus.Configured, target.Status));
        var registration = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.AspireSkills && target.Scope is AgentConfigurationScope.Global);
        var skill = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.DotnetInspect && target.Scope is AgentConfigurationScope.Global);
        Assert.Equal(AgentPath.Resolve(Path.Combine(expectedDirectory, "settings.json")),
            AgentPath.Resolve(registration.TargetPath));
        Assert.Equal(AgentPath.Resolve(Path.Combine(expectedDirectory, "skills", "dotnet-inspect")),
            AgentPath.Resolve(skill.TargetPath));
        Assert.Equal(CommonAgentApplicators.DotnetInspectSkillFileContent,
            await File.ReadAllTextAsync(Path.Combine(skill.TargetPath, "SKILL.md")).DefaultTimeout());
        Assert.Empty(_context.Project.EnumerateFileSystemInfos());
        Assert.Equal(0, npm.ResolveCallCount);
        Assert.Equal(0, npm.PackCallCount);
        Assert.Equal(0, npm.InstallGlobalCallCount);
        Assert.Equal(0, playwright.GetVersionCallCount);
        Assert.Equal(0, playwright.InstallSkillsCallCount);
    }
    public void Dispose() => _context.Dispose();

}
