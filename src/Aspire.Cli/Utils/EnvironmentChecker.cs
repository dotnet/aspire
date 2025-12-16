// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Orchestrates environment checks using injected IEnvironmentCheck instances.
/// </summary>
internal sealed class EnvironmentChecker(IEnumerable<IEnvironmentCheck> checks) : IEnvironmentChecker
{
    private readonly IEnvironmentCheck[] _checks = checks.OrderBy(c => c.Order).ToArray();

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EnvironmentCheckResult>();

        // Run all checks in order (by Order property)
        foreach (var check in _checks)
        {
            var result = await check.CheckAsync(cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
