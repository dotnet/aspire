// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

/// <summary>
/// Isolates native project, user, and managed-policy files from the test host.
/// </summary>
internal sealed class AgentConfigurationTestContext : IDisposable
{
    private readonly Dictionary<string, string?> _variables;

    public AgentConfigurationTestContext(ITestOutputHelper output)
    {
        Workspace = TemporaryWorkspace.Create(output);
        Project = Workspace.CreateDirectory("project");
        Home = Workspace.CreateDirectory("home");
        _variables = new()
        {
            ["ProgramFiles"] = Path.Combine(Workspace.WorkspaceRoot.FullName, "system"),
            ["ProgramData"] = Path.Combine(Workspace.WorkspaceRoot.FullName, "system-data")
        };
        // Keep platform-specific policy reads inside the fixture on every test OS.
        Environment = TestEnvironment.CreateWindows(_variables);
        ExecutionContext = TestExecutionContextHelper.CreateExecutionContext(Project, homeDirectory: Home);
    }

    public TemporaryWorkspace Workspace { get; }
    public DirectoryInfo Project { get; }
    public DirectoryInfo Home { get; }
    public TestEnvironment Environment { get; }
    public CliExecutionContext ExecutionContext { get; }
    public IReadOnlyList<AgentClient> Environments { get; } = AgentClient.All;
    public AgentClient Copilot { get; } = AgentClient.Copilot;
    public AgentClient ClaudeCode { get; } = AgentClient.ClaudeCode;
    public AgentClient OpenCode { get; } = AgentClient.OpenCode;
    public string CopilotDirectory => Copilot.GlobalPluginDirectory(ExecutionContext, Environment);
    public string ClaudeDirectory => ClaudeCode.GlobalPluginDirectory(ExecutionContext, Environment);
    public string ClaudeManagedDirectory => ClaudeCode.ManagedPluginDirectory(ExecutionContext, Environment);
    public string OpenCodeDirectory => OpenCode.GlobalPluginDirectory(ExecutionContext, Environment);

    public void SetVariable(string name, string value) => _variables[name] = value;

    public Task<IReadOnlyList<AgentTargetResult>> ConfigureNativeAsync(
        AgentConfigurationScope scope, IReadOnlyList<AgentClient> clients, CancellationToken cancellationToken = default)
        => AspireSkillsPluginConfiguration.ConfigureAsync(Project, scope, clients, ExecutionContext, Environment, cancellationToken);

    public static async Task WriteAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    public void Dispose() => Workspace.Dispose();
}
