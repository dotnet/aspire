// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

/// <summary>
/// Shares resource selection and telemetry-only resource names between the resources layout and main navigation.
/// </summary>
public sealed class ResourcePaneState
{
    private HashSet<string> _telemetryOnlyResourceNames = new(StringComparers.ResourceName);

    internal IReadOnlyList<string> SelectedResourceNames { get; private set; } = [];

    internal IReadOnlyList<string> ExcludeTelemetryOnlyResources(IReadOnlyList<string> resourceNames) =>
        resourceNames.Where(name => !_telemetryOnlyResourceNames.Contains(name)).ToList();

    internal void SetSelectedResources(IReadOnlyList<string> resourceNames)
    {
        if (SelectedResourceNames.SequenceEqual(resourceNames, StringComparers.ResourceName))
        {
            return;
        }

        SelectedResourceNames = resourceNames;
        Changed?.Invoke();
    }

    /// <summary>
    /// Raised when the selection or telemetry-only resources change.
    /// </summary>
    public event Action? Changed;

    internal void SetTelemetryOnlyResources(IEnumerable<string> resourceNames)
    {
        var names = resourceNames.ToHashSet(StringComparers.ResourceName);
        if (_telemetryOnlyResourceNames.SetEquals(names))
        {
            return;
        }

        _telemetryOnlyResourceNames = names;
        Changed?.Invoke();
    }
}
