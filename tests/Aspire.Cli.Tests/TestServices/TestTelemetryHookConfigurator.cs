// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Agents.Hooks;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestTelemetryHookConfigurator : ITelemetryHookConfigurator
{
    public Exception? Exception { get; init; }
    public IReadOnlyCollection<AgentClientKind> ConfiguredClients { get; private set; } = [];

    public Task<TelemetryHookConfigurationResult> ConfigureAsync(
        IReadOnlyCollection<AgentClientKind> detectedClients,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is { } exception)
        {
            throw exception;
        }

        ConfiguredClients = detectedClients.ToArray();
        return Task.FromResult(new TelemetryHookConfigurationResult(ConfiguredClients.ToArray(), []));
    }
}
