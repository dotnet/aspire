// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAgentEnvironmentDetector : IAgentEnvironmentDetector
{
    private int _scanCount;

    public int ScanCount => _scanCount;

    public AgentEnvironment DetectedEnvironments { get; init; }

    public Task<AgentDiscovery> DetectAsync(
        AgentEnvironmentScanContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _scanCount);
        var detections = Enum.GetValues<AgentEnvironment>()
            .Where(value => value is not AgentEnvironment.None && DetectedEnvironments.HasFlag(value))
            .Select(value => new AgentClientDetection(value, null)).ToArray();
        return Task.FromResult(new AgentDiscovery(
            TestAgentEnvironmentScanner.CreateEnvironments(detections).Where(scanner => scanner.Client.Includes(DetectedEnvironments)).ToArray(),
            detections));
    }
}
