// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Tracks unsecured transport validation warnings that should be presented to the user.
/// </summary>
internal sealed class UnsecuredTransportWarning
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
                return _warnings.Count > 0;
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
            _warnings.Add(message);
        }
    }

    /// <summary>
    /// Gets or sets whether the user has explicitly chosen to continue despite warnings.
    /// </summary>
    public bool UserAcceptedRisk { get; set; }
}
