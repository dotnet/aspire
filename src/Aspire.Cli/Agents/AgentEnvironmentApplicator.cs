// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Represents an agent environment that was detected and can be configured.
/// </summary>
internal sealed class AgentEnvironmentApplicator
{
    private readonly Func<CancellationToken, Task> _applyCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentEnvironmentApplicator"/> class.
    /// </summary>
    /// <param name="description">The description shown in selection prompts.</param>
    /// <param name="fingerprint">The unique fingerprint for storing user preferences.</param>
    /// <param name="applyCallback">The callback to apply the configuration.</param>
    public AgentEnvironmentApplicator(string description, string fingerprint, Func<CancellationToken, Task> applyCallback)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(applyCallback);

        Description = description;
        Fingerprint = fingerprint;
        _applyCallback = applyCallback;
    }

    /// <summary>
    /// Gets the description of the agent environment shown in the selection prompt.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the unique fingerprint for this applicator.
    /// Used to store user preferences about whether they've declined to enable this environment.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Applies the configuration changes to enable the agent environment.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await _applyCallback(cancellationToken);
    }
}
