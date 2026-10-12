// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class TelemetryHookConfiguratorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Plan_WritesDetectedCopilotUserHook()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.Copilot],
            detections: [new(AgentEnvironment.CopilotCli, null)]);

        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        Assert.Equal([context.Copilot], hook.Environments);
        Assert.Equal(AgentConfigurationScope.Global, hook.Scope);
        Assert.Equal(1, context.HookInstaller.Calls);

        var root = await ReadObjectAsync(hook.TargetPath).DefaultTimeout();
        Assert.Equal(1, (int)root["version"]!);
        var entry = Assert.Single(root["hooks"]!["postToolUse"]!.AsArray())!.AsObject();
        Assert.Equal(["type", "exec", "args", "timeoutSec"], entry.Select(property => property.Key));
        Assert.Equal("command", (string?)entry["type"]);
        Assert.Equal(30, (int)entry["timeoutSec"]!);
        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        Assert.Equal(command, (string?)entry["exec"]);
        Assert.Equal(args, entry["args"]!.AsArray().Select(value => (string)value!));
    }

    [Fact]
    public async Task Plan_RegistersOneSharedCopilotHookForDetectedAppAndCli()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        IAgentEnvironmentScanner[] clients = [context.Copilot];

        var request = context.Request(AgentConfigurationScope.Global, clients, detections:
        [
            new(AgentEnvironment.CopilotApp, null),
            new(AgentEnvironment.CopilotCli, null),
            new(AgentEnvironment.CopilotCli, null)
        ]);

        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(clients, hook.Environments);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        Assert.Equal(1, context.HookInstaller.Calls);
    }

    [Fact]
    public async Task Plan_HonorsCopilotHomeForBothRegistrationAndHook()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var custom = context.Workspace.CreateDirectory("custom-copilot");
        context.SetVariable("COPILOT_HOME", custom.FullName);

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.Copilot], detections: [new(AgentEnvironment.CopilotCli, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(Path.Combine(custom.FullName, "hooks", "aspire-telemetry.json"), hook.TargetPath);
        Assert.True(File.Exists(Path.Combine(custom.FullName, "settings.json")));
        Assert.True(File.Exists(hook.TargetPath));
        Assert.False(Directory.Exists(Path.Combine(context.Home.FullName, ".copilot")));
    }

    [Fact]
    public async Task Plan_WritesClaudeUserHookWithExistingExecFormAndTimeout()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.ClaudeCode], detections: [new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Configured, hook.Status);
        var root = await ReadObjectAsync(hook.TargetPath).DefaultTimeout();
        var group = Assert.Single(root["hooks"]!["PostToolUse"]!.AsArray())!.AsObject();
        Assert.Equal("*", (string?)group["matcher"]);
        var entry = Assert.Single(group["hooks"]!.AsArray())!.AsObject();
        Assert.Equal("command", (string?)entry["type"]);
        Assert.Equal(30, (int)entry["timeout"]!);

        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        Assert.Equal(command, (string?)entry["command"]);
        Assert.Equal(args, entry["args"]!.AsArray().Select(value => (string)value!));
    }

    [Fact]
    public async Task Plan_PreservesExistingClaudeHooksAndSettings()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var path = Path.Combine(context.ClaudeDirectory, "settings.json");
        await AgentConfigurationTestContext.WriteAsync(path, """
            {
              "model": "preserved",
              "hooks": {
                "PostToolUse": [{
                  "matcher": "Write",
                  "hooks": [{ "type": "command", "command": "echo existing" }]
                }]
              }
            }
            """).DefaultTimeout();

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.ClaudeCode], detections: [new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        Assert.Equal(AgentConfigurationStatus.Configured, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).Status);
        var root = await ReadObjectAsync(path).DefaultTimeout();
        Assert.Equal("preserved", (string?)root["model"]);
        var groups = root["hooks"]!["PostToolUse"]!.AsArray();
        Assert.Equal(2, groups.Count);
        Assert.Equal("Write", (string?)groups[0]!["matcher"]);
        Assert.Equal("echo existing", (string?)groups[0]!["hooks"]![0]!["command"]);
        Assert.Equal("*", (string?)groups[1]!["matcher"]);
    }

    [Theory]
    [InlineData("{ this is not valid json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("null")]
    [InlineData("""{"version":2}""")]
    [InlineData("""{"hooks":"not-an-object"}""")]
    public async Task Plan_BlocksMalformedCopilotHookFileWithoutUndoingNativeRegistration(string existing)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var path = Path.Combine(context.CopilotDirectory, "hooks", "aspire-telemetry.json");
        await AgentConfigurationTestContext.WriteAsync(path, existing).DefaultTimeout();

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.Copilot], detections: [new(AgentEnvironment.CopilotCli, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        Assert.Equal(AgentConfigurationStatus.Blocked, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).Status);
        Assert.False(result.HasErrors);
        Assert.True(result.HasWarnings);
        Assert.Equal(0, context.HookInstaller.Calls);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
    }

    [Theory]
    [InlineData("""{"hooks":"not-an-object"}""")]
    [InlineData("""{"hooks":{"PostToolUse":{}}}""")]
    [InlineData("""{"hooks":{"PostToolUse":[{"hooks":{}}]}}""")]
    public async Task Plan_BlocksUnexpectedClaudeHookShapeAfterUnchangedNativeRegistration(string hookSettings)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.ClaudeCode],
            detections: [new(AgentEnvironment.ClaudeCode, null)]);
        await context.ConfigureNativeAsync(request).DefaultTimeout();
        var path = Path.Combine(context.ClaudeDirectory, "settings.json");
        var root = await ReadObjectAsync(path).DefaultTimeout();
        root["hooks"] = JsonNode.Parse(hookSettings)!["hooks"]!.DeepClone();
        var existing = root.ToJsonString();
        await File.WriteAllTextAsync(path, existing).DefaultTimeout();

        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        Assert.Equal(AgentConfigurationStatus.Blocked, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).Status);
        Assert.Equal(0, context.HookInstaller.Calls);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
    }

    [Fact]
    public async Task Plan_DetectedClientsDoNotRequireNativeConfiguration()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.OpenCode],
            detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)]);

        var results = await context.Writer.ApplyAsync(context.Hooks.Plan(request), CancellationToken.None).DefaultTimeout();

        Assert.Equal(2, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal(AgentConfigurationStatus.Configured, result.Status);
            Assert.Equal(AgentConfigurationScope.Global, result.Scope);
            Assert.Null(result.Message);
        });
        Assert.Equal(1, context.HookInstaller.Calls);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.False(File.Exists(Path.Combine(context.CopilotDirectory, "settings.json")));
        var claude = await ReadObjectAsync(Path.Combine(context.ClaudeDirectory, "settings.json")).DefaultTimeout();
        Assert.Equal(["hooks"], claude.Select(property => property.Key));
    }

    [Fact]
    public async Task Plan_SelectedButUndetectedClientsDoNotReceiveHooks()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.Copilot, context.ClaudeCode],
            detections: [new(AgentEnvironment.VsCode, null), new(AgentEnvironment.OpenCode, null)]);

        var results = await context.Writer.ApplyAsync(context.Hooks.Plan(request), CancellationToken.None).DefaultTimeout();

        Assert.Empty(results);
        Assert.Equal(0, context.HookInstaller.Calls);
        Assert.Empty(context.Project.EnumerateFileSystemInfos());
        Assert.Empty(context.Home.EnumerateFileSystemInfos());
    }

    [Fact]
    public void Plan_OnlyCreatesTargetsForDetectedSupportedClientsDuringSetup()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);

        Assert.Empty(context.Hooks.Plan(context.Request(AgentConfigurationScope.Global, [context.Copilot, context.OpenCode],
            detections: [new(AgentEnvironment.VsCode, null), new(AgentEnvironment.OpenCode, null)])));
        Assert.Empty(context.Hooks.Plan(context.Request(AgentConfigurationScope.Project, [context.Copilot, context.ClaudeCode],
            detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)])));
        Assert.Empty(context.Hooks.Plan(context.Request(AgentConfigurationScope.Global, [context.Copilot, context.ClaudeCode],
            skills: false, detections: [new(AgentEnvironment.CopilotCli, null), new(AgentEnvironment.ClaudeCode, null)])));
        Assert.Empty(context.Hooks.Plan(context.Request(AgentConfigurationScope.Global, [], detections: [new(AgentEnvironment.ClaudeCode, null)])));
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    [Theory]
    [InlineData("GitHub Copilot")]
    [InlineData("Claude Code")]
    public async Task Plan_PreservesBytesAndTimestampOnRepeat(string clientName)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var client = context.Environments.Single(client => client.DisplayName == clientName);
        var detectedClient = clientName == "GitHub Copilot" ? AgentEnvironment.CopilotCli : AgentEnvironment.ClaudeCode;
        var request = context.Request(AgentConfigurationScope.Global, [client], detections: [new(detectedClient, null)]);
        var first = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();
        var path = Assert.Single(first.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).TargetPath;
        var bytes = await File.ReadAllBytesAsync(path).DefaultTimeout();
        File.SetLastWriteTimeUtc(path, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(path);

        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets, target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path).DefaultTimeout());
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task Plan_HonorsClaudeConfigDirectoryAndJsonc()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        context.SetVariable("CLAUDE_CONFIG_DIR", @"~\claude-work");
        var path = Path.Combine(context.Home.FullName, "claude-work", "settings.json");
        await AgentConfigurationTestContext.WriteAsync(path, """{/* comment */"model":"preserved",}""").DefaultTimeout();

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.ClaudeCode], detections: [new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        Assert.Equal(path, Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks).TargetPath);
        var root = await ReadObjectAsync(path).DefaultTimeout();
        Assert.Equal("preserved", (string?)root["model"]);
        Assert.Single(root["hooks"]!["PostToolUse"]!.AsArray());
        Assert.False(Directory.Exists(Path.Combine(context.Home.FullName, ".claude")));
        Assert.False(Directory.Exists(Path.Combine(context.Project.FullName, "~")));
    }

    [Fact]
    public async Task Plan_PreservesThirdPartyHooksWithTheSameScriptName()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var path = Path.Combine(context.ClaudeDirectory, "settings.json");
        const string command = "bash /vendor/other-product/track-telemetry.sh";
        await AgentConfigurationTestContext.WriteAsync(path, """
            {
              "hooks": {
                "PostToolUse": [{
                  "matcher": "*",
                  "hooks": [{ "type": "command", "command": "bash /vendor/other-product/track-telemetry.sh" }]
                }]
              }
            }
            """).DefaultTimeout();

        var result = await context.ConfigureAsync(
            context.Request(AgentConfigurationScope.Global, [context.ClaudeCode], detections: [new(AgentEnvironment.ClaudeCode, null)]),
            CancellationToken.None).DefaultTimeout();

        AssertNativeConfigured(result);
        var root = await ReadObjectAsync(path).DefaultTimeout();
        var groups = root["hooks"]!["PostToolUse"]!.AsArray();
        Assert.Equal(2, groups.Count);
        Assert.Equal(command, (string?)groups[0]!["hooks"]![0]!["command"]);
        Assert.Equal(AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName).Command, (string?)groups[1]!["hooks"]![0]!["command"]);
    }

    [Theory]
    [InlineData("GitHub Copilot", "notaspire.dll", false)]
    [InlineData("GitHub Copilot", "aspire.dll", true)]
    [InlineData("GitHub Copilot", "ASPIRE.DLL", true)]
    [InlineData("Claude Code", "notaspire.dll", false)]
    [InlineData("Claude Code", "aspire.dll", true)]
    [InlineData("Claude Code", "ASPIRE.DLL", true)]
    public async Task Plan_MigratesOnlyExactManagedAspireAssembly(string clientName, string assemblyName, bool shouldReplace)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var client = context.Environments.Single(client => client.DisplayName == clientName);
        var isCopilot = clientName == "GitHub Copilot";
        var path = isCopilot
            ? Path.Combine(context.CopilotDirectory, "hooks", "aspire-telemetry.json")
            : Path.Combine(context.ClaudeDirectory, "settings.json");
        var host = string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)
            ? Environment.ProcessPath!
            : Path.Combine(context.Workspace.Path, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var existingHook = new JsonObject
        {
            ["type"] = "command",
            [isCopilot ? "exec" : "command"] = host,
            ["args"] = new JsonArray(Path.Combine(context.Workspace.Path, assemblyName), "agent", "telemetry", "--hook")
        };
        var entries = isCopilot
            ? new JsonArray(existingHook)
            : new JsonArray(new JsonObject
            {
                ["matcher"] = "*",
                ["hooks"] = new JsonArray(existingHook)
            });
        var root = new JsonObject
        {
            ["hooks"] = new JsonObject { [isCopilot ? "postToolUse" : "PostToolUse"] = entries }
        };
        if (isCopilot)
        {
            root["version"] = 1;
        }
        await AgentConfigurationTestContext.WriteAsync(path, root.ToJsonString()).DefaultTimeout();
        var request = context.Request(AgentConfigurationScope.Global, [client],
            detections: [new(isCopilot ? AgentEnvironment.CopilotCli : AgentEnvironment.ClaudeCode, null)]);

        await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();
        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets, target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        var configured = await ReadObjectAsync(path).DefaultTimeout();
        var hooks = isCopilot
            ? configured["hooks"]!["postToolUse"]!.AsArray().ToArray()
            : configured["hooks"]!["PostToolUse"]!.AsArray().SelectMany(group => group!["hooks"]!.AsArray()).ToArray();
        Assert.Equal(shouldReplace ? 1 : 2, hooks.Length);
        Assert.Equal(!shouldReplace, hooks.Any(hook => JsonNode.DeepEquals(hook, existingHook)));
        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        Assert.Single(hooks, hook => (string?)hook![isCopilot ? "exec" : "command"] == command
            && hook["args"]!.AsArray().Select(value => (string)value!).SequenceEqual(args));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Plan_ExistingProjectHookRefreshesScriptsWithoutDuplicateUserRegistration(bool deleteScripts, bool nativeHook)
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.ClaudeCode],
            detections: [new(AgentEnvironment.ClaudeCode, null)]);
        await context.ConfigureNativeAsync(request).DefaultTimeout();
        await context.ConfigureNativeAsync(request with { Scope = AgentConfigurationScope.Project }).DefaultTimeout();
        var projectPath = Path.Combine(context.Project.FullName, ".claude", "settings.json");
        var root = await ReadObjectAsync(projectPath).DefaultTimeout();
        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        root["hooks"] = new JsonObject
        {
            ["PostToolUse"] = new JsonArray(new JsonObject
            {
                ["matcher"] = "*",
                ["hooks"] = new JsonArray(new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = nativeHook ? command : "bash",
                    ["args"] = nativeHook
                        ? new JsonArray(args.Select(arg => (JsonNode?)JsonValue.Create(arg)).ToArray())
                        : new JsonArray(Path.Combine(context.ExecutionContext.AspireHomeDirectory.FullName, "hooks", "track-telemetry.sh"))
                })
            })
        };
        var existing = root.ToJsonString();
        await File.WriteAllTextAsync(projectPath, existing).DefaultTimeout();

        var installer = new TelemetryHookInstaller(context.ExecutionContext, NullLogger<TelemetryHookInstaller>.Instance);
        var scripts = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();
        var shellBytes = await File.ReadAllBytesAsync(scripts.ShellScriptPath).DefaultTimeout();
        var powerShellBytes = await File.ReadAllBytesAsync(scripts.PowerShellScriptPath).DefaultTimeout();
        if (deleteScripts)
        {
            File.Delete(scripts.ShellScriptPath);
            File.Delete(scripts.PowerShellScriptPath);
        }
        else
        {
            await File.WriteAllTextAsync(scripts.ShellScriptPath, "outdated shell script").DefaultTimeout();
            await File.WriteAllTextAsync(scripts.PowerShellScriptPath, "outdated PowerShell script").DefaultTimeout();
        }
        var hooks = new TelemetryHookConfigurator(installer, context.Environments, context.ExecutionContext, NullLogger<TelemetryHookConfigurator>.Instance);
        var result = await context.Setup.ConfigureAsync(request, hooks, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Skipped, hook.Status);
        Assert.Equal(AgentCommandStrings.Configuration_ExistingProjectHook, hook.Message);
        Assert.Equal(existing, await File.ReadAllTextAsync(projectPath).DefaultTimeout());
        Assert.Null((await ReadObjectAsync(hook.TargetPath).DefaultTimeout())["hooks"]);
        Assert.Equal(shellBytes, await File.ReadAllBytesAsync(scripts.ShellScriptPath).DefaultTimeout());
        Assert.Equal(powerShellBytes, await File.ReadAllBytesAsync(scripts.PowerShellScriptPath).DefaultTimeout());
    }

    [Fact]
    public async Task Plan_RespectsExplicitHookDisablement()
    {
        using var context = new AgentConfigurationTestContext(outputHelper);
        var request = context.Request(AgentConfigurationScope.Global, [context.ClaudeCode],
            detections: [new(AgentEnvironment.ClaudeCode, null)]);
        await context.ConfigureNativeAsync(request).DefaultTimeout();
        var path = Path.Combine(context.ClaudeDirectory, "settings.json");
        var root = await ReadObjectAsync(path).DefaultTimeout();
        root["disableAllHooks"] = true;
        var existing = root.ToJsonString();
        await File.WriteAllTextAsync(path, existing).DefaultTimeout();

        var result = await context.ConfigureAsync(request, CancellationToken.None).DefaultTimeout();

        Assert.All(result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills),
            target => Assert.Equal(AgentConfigurationStatus.Unchanged, target.Status));
        var hook = Assert.Single(result.Targets, target => target.Asset is AgentAssetKind.TelemetryHooks);
        Assert.Equal(AgentConfigurationStatus.Skipped, hook.Status);
        Assert.Equal(AgentCommandStrings.Configuration_PolicyBlocked, hook.Message);
        Assert.Equal(existing, await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Equal(0, context.HookInstaller.Calls);
    }

    private static void AssertNativeConfigured(AgentInitResult result)
    {
        Assert.False(result.HasErrors);
        var native = result.Targets.Where(target => target.Asset is AgentAssetKind.AspireSkills).ToArray();
        Assert.NotEmpty(native);
        Assert.All(native, target => Assert.Equal(AgentConfigurationStatus.Configured, target.Status));
    }

    private static async Task<JsonObject> ReadObjectAsync(string path)
        => JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
}
