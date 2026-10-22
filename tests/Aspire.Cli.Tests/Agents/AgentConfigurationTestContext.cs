// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.Copilot;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

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
        Copilot = new CopilotAgentClient(ExecutionContext, Environment);
        ClaudeCode = new ClaudeCodeAgentClient(ExecutionContext, Environment);
        OpenCode = new OpenCodeAgentClient(ExecutionContext, Environment);
        Clients = [Copilot, ClaudeCode, OpenCode];
    }

    public TemporaryWorkspace Workspace { get; }
    public DirectoryInfo Project { get; }
    public DirectoryInfo Home { get; }
    public TestEnvironment Environment { get; }
    public CliExecutionContext ExecutionContext { get; }
    public IReadOnlyList<IAgentClient> Clients { get; }
    public IAgentClient Copilot { get; }
    public IAgentClient ClaudeCode { get; }
    public IAgentClient OpenCode { get; }
    public string CopilotDirectory => AgentPath.Expand(CopilotPaths.GetConfigDirectory(Home, Environment), ExecutionContext);
    public string ClaudeDirectory => ClaudeCodePaths.GetConfigDirectory(ExecutionContext, Environment);
    public string ClaudeManagedDirectory => AgentPath.GetManagedSettingsDirectory(ExecutionContext, Environment, "ClaudeCode", "claude-code");
    public string OpenCodeDirectory => OpenCodePaths.GetConfigDirectory(ExecutionContext, Environment);

    public void SetVariable(string name, string value) => _variables[name] = value;

    public IServiceCollection CreateServiceCollection(
        ITestOutputHelper output, Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var services = CliTestHelper.CreateServiceCollection(Workspace, output, options =>
        {
            options.CliExecutionContextFactory = _ => ExecutionContext;
            configure?.Invoke(options);
        });
        services.AddSingleton<IEnvironment>(Environment);

        return services;
    }

    public Task<IReadOnlyList<AgentTargetResult>> ConfigureIntegrationAsync(
        AgentConfigurationScope scope, IReadOnlyList<IAgentClient> clients, CancellationToken cancellationToken = default)
        => AgentConfiguration.ApplyNativeAsync(clients, Project, scope, static () => new AgentConfiguration(), cancellationToken);

    public async Task<(string Path, bool Repaired, bool Added)> ConfigureMcpAsync(
        IAgentClient client, AgentConfigurationScope scope, CancellationToken cancellationToken = default)
    {
        var edit = new AgentConfiguration();
        var (config, path) = await client.ReadMcpConfigAsync(
            client.GetMcpConfigPath(scope, Project), Project, scope, edit, cancellationToken);
        var (repaired, added) = client.UpdateMcpConfig(config, path, configureServer: true, scope);
        if (repaired || added)
        {
            await edit.PublishAsync(path, config, cancellationToken);
        }

        return (path, repaired, added);
    }

    public static async Task WriteAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    public void Dispose() => Workspace.Dispose();
}
