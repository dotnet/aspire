// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Detects agent environments by running all registered scanners.
/// </summary>
internal sealed class AgentEnvironmentDetector(IEnumerable<IAgentEnvironmentScanner> scanners) : IAgentEnvironmentDetector
{
    /// <inheritdoc />
    public async Task<AgentDiscovery> DetectAsync(
        AgentEnvironmentScanContext context,
        CancellationToken cancellationToken)
    {
        var environments = new List<IAgentEnvironmentScanner>();
        var detections = new List<AgentClientDetection>();
        foreach (var scanner in scanners)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detected = await scanner.ScanAsync(context, cancellationToken);
            if (detected.Count > 0)
            {
                environments.Add(scanner);
                detections.AddRange(detected);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new(Array.AsReadOnly(environments.ToArray()), Array.AsReadOnly(detections.Distinct().ToArray()));
    }
}
