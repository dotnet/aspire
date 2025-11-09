// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Tracks unsecured transport validation warnings that should be presented to the user.
/// </summary>
internal sealed class UnsecuredTransportWarning(ILogger<UnsecuredTransportWarning> logger)
{
    private readonly List<string> _warnings = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets a value indicating whether there are any unsecured transport warnings.
    /// </summary>
    public bool HasWarnings
    {
        get
        {
            lock (_lock)
            {
                var hasWarnings = _warnings.Count > 0;
                logger.LogDebug("UnsecuredTransportWarning.HasWarnings: {HasWarnings}, Count: {Count}", hasWarnings, _warnings.Count);
                return hasWarnings;
            }
        }
    }

    /// <summary>
    /// Gets all collected warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings
    {
        get
        {
            lock (_lock)
            {
                return _warnings.ToList();
            }
        }
    }

    /// <summary>
    /// Adds a warning message.
    /// </summary>
    public void AddWarning(string message)
    {
        lock (_lock)
        {
            logger.LogInformation("UnsecuredTransportWarning.AddWarning: Adding warning - {Message}", message);
            _warnings.Add(message);
            logger.LogDebug("UnsecuredTransportWarning.AddWarning: Total warnings now: {Count}", _warnings.Count);
        }
    }

    /// <summary>
    /// Gets or sets whether the user has explicitly chosen to continue despite warnings.
    /// </summary>
    public bool UserAcceptedRisk { get; set; }
}
