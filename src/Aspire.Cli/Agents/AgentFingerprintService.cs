// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Agents;

/// <summary>
/// Service for tracking acknowledged agent environment applicators using global configuration.
/// </summary>
internal sealed class AgentFingerprintService(IConfigurationService configurationService) : IAgentFingerprintService
{
    private const string FingerprintConfigPrefix = "firstrun.fingerprints.agents";

    /// <inheritdoc />
    public async Task<bool> IsAcknowledgedAsync(AgentEnvironmentApplicator applicator, CancellationToken cancellationToken)
    {
        var configKey = $"{FingerprintConfigPrefix}.{applicator.Fingerprint}";
        var value = await configurationService.GetConfigurationAsync(configKey, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrEmpty(value);
    }

    /// <inheritdoc />
    public async Task<AgentEnvironmentApplicator[]> FilterAcknowledgedAsync(IEnumerable<AgentEnvironmentApplicator> applicators, CancellationToken cancellationToken)
    {
        var result = new List<AgentEnvironmentApplicator>();

        foreach (var applicator in applicators)
        {
            if (!await IsAcknowledgedAsync(applicator, cancellationToken).ConfigureAwait(false))
            {
                result.Add(applicator);
            }
        }

        return [.. result];
    }

    /// <inheritdoc />
    public async Task RecordAcknowledgedAsync(IEnumerable<AgentEnvironmentApplicator> applicators, CancellationToken cancellationToken)
    {
        var version = VersionHelper.GetDefaultTemplateVersion();

        foreach (var applicator in applicators)
        {
            var configKey = $"{FingerprintConfigPrefix}.{applicator.Fingerprint}";
            await configurationService.SetConfigurationAsync(configKey, version, isGlobal: true, cancellationToken).ConfigureAwait(false);
        }
    }
}
