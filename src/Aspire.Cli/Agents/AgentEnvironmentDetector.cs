// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Detects agent environments by running all registered scanners.
/// </summary>
internal sealed class AgentEnvironmentDetector : IAgentEnvironmentDetector
{
    private readonly IEnumerable<IAgentEnvironmentScanner> _scanners;

    public AgentEnvironmentDetector(IEnumerable<IAgentEnvironmentScanner> scanners)
    {
        _scanners = scanners;
    }

    /// <inheritdoc />
    public async Task<AgentEnvironmentApplicator[]> DetectAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workingDirectory
        };

        foreach (var scanner in _scanners)
        {
            await scanner.ScanAsync(context, cancellationToken);
        }

        return [.. context.Applicators];
    }
}
