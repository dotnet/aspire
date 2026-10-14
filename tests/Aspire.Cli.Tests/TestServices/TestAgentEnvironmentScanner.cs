// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Cli.Agents;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private readonly IReadOnlyList<AgentClientDetection> _detections;
    private readonly ConcurrentQueue<(DirectoryInfo WorkingDirectory, DirectoryInfo? WorkspaceRoot, CancellationToken CancellationToken)> _calls = new();

    public TestAgentEnvironmentScanner(AgentClient client, params AgentClientDetection[] detections)
    {
        Client = client;
        _detections = Array.AsReadOnly(detections.ToArray());
    }

    public AgentClient Client { get; }
    public string DisplayName => Client.DisplayName;
    public IReadOnlyList<(DirectoryInfo WorkingDirectory, DirectoryInfo? WorkspaceRoot, CancellationToken CancellationToken)> Calls => _calls.ToArray();

    public Func<AgentEnvironmentScanContext, CancellationToken, Task<IReadOnlyList<AgentClientDetection>>>? ScanAsyncCallback { get; set; }

    public Func<AgentInitRequest, IEnumerable<AgentConfigurationTarget>>? GetTargetsCallback { get; set; }

    public static TestAgentEnvironmentScanner[] CreateEnvironments(params AgentClientDetection[] detections) =>
    [
        new(AgentClient.Copilot, detections.Where(d => AgentClient.Copilot.Includes(d.Client)).ToArray()),
        new(AgentClient.ClaudeCode, detections.Where(d => AgentClient.ClaudeCode.Includes(d.Client)).ToArray()),
        new(AgentClient.OpenCode, detections.Where(d => AgentClient.OpenCode.Includes(d.Client)).ToArray())
    ];

    public Task<IReadOnlyList<AgentClientDetection>> ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Enqueue((context.WorkingDirectory, context.RepositoryRoot, cancellationToken));

        if (ScanAsyncCallback is not null)
        {
            return ScanAsyncCallback(context, cancellationToken);
        }
        return Task.FromResult(_detections);
    }

    public IEnumerable<AgentConfigurationTarget> GetTargets(AgentInitRequest request)
        => GetTargetsCallback?.Invoke(request) ?? [];

    public AgentConfigurationTarget CreateTarget(string path, AgentAssetKind asset, AgentConfigurationStatus status, string? message)
        => new(path, AgentConfigurationScope.Project, asset, [this], asset.ToString(), (root, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (status is AgentConfigurationStatus.Configured)
            {
                root["configured"] = true;
            }
            return Task.FromResult(new AgentConfigurationEdit(status, message));
        });

    public override string ToString() => DisplayName;
}
