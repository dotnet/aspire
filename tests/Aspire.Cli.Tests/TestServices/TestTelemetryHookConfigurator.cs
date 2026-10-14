// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestTelemetryHookConfigurator : ITelemetryHookConfigurator
{
    public ConcurrentQueue<AgentInitRequest> Requests { get; } = new();

    public AgentEnvironment ConfiguredEnvironments => Requests
        .Where(request => request.Scope is AgentConfigurationScope.Global)
        .SelectMany(request => request.Detections
            .Where(detection => request.Environments.Any(scanner => scanner.Client.Includes(detection.Client))))
        .Aggregate(AgentEnvironment.None, (environments, detection) => environments | detection.Client);

    public Func<AgentInitRequest, IEnumerable<AgentConfigurationTarget>>? PlanCallback { get; set; }

    public IEnumerable<AgentConfigurationTarget> Plan(AgentInitRequest request)
    {
        Requests.Enqueue(request);
        return PlanCallback?.Invoke(request) ?? [];
    }
}
