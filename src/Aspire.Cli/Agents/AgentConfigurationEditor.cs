// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Cli.Agents;

internal static class AgentConfigurationEditor
{
    public static async Task<IReadOnlyList<AgentTargetResult>> ApplyAsync(
        IEnumerable<IAgentClient> clients, DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<AgentTargetResult>();
        foreach (var targets in clients.Distinct().GroupBy(client => Path.GetFullPath(client.GetIntegrationConfigPath(scope, workspaceRoot)), AgentPath.Comparer))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = AgentConfigurationStatus.Configured;
            string? message = null;
            try
            {
                var reads = new AgentConfigurationReadContext();
                var original = await AgentConfigurationJson.ReadAsync(targets.Key, reads, cancellationToken);
                var root = original?.DeepClone().AsObject() ?? new JsonObject();
                foreach (var client in targets)
                {
                    await client.UpdateIntegrationConfigAsync(root, workspaceRoot, scope, reads, cancellationToken);
                }

                if (JsonNode.DeepEquals(original ?? new JsonObject(), root))
                {
                    await reads.ValidateAsync(targets.Key, cancellationToken);
                    status = AgentConfigurationStatus.Unchanged;
                }
                else
                {
                    await AgentConfigurationFile.WriteAsync(targets.Key, root, reads, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                status = AgentConfigurationStatus.Failed;
                message = ex.Message;
            }

            results.Add(new(targets.ToArray(), targets.Key, status, message));
        }

        return results;
    }
}

internal enum AgentConfigurationStatus
{
    Configured,
    Unchanged,
    Failed
}

internal sealed record AgentTargetResult(
    IReadOnlyList<IAgentClient> Clients,
    string TargetPath,
    AgentConfigurationStatus Status,
    string? Message);
