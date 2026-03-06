// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agents;

/// <summary>
/// Scans for the industry-standard <c>.agents/skills/</c> skill file locations
/// at both the workspace level and the user's home directory.
/// </summary>
internal sealed class StandardLocationAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private readonly CliExecutionContext _executionContext;
    private readonly ILogger<StandardLocationAgentEnvironmentScanner> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="StandardLocationAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="executionContext">The CLI execution context for accessing the home directory.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public StandardLocationAgentEnvironmentScanner(CliExecutionContext executionContext, ILogger<StandardLocationAgentEnvironmentScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(logger);
        _executionContext = executionContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting standard location skill file scan");

        CommonAgentApplicators.TryAddStandardLocationSkillFileApplicators(
            context,
            context.RepositoryRoot,
            _executionContext.HomeDirectory);

        return Task.CompletedTask;
    }
}
