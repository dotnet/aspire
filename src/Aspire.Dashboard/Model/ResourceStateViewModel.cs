// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Resources;
using Humanizer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

internal class ResourceStateViewModel(string text, Icon icon, Color color)
{
    public string Text { get; } = text;
    public Icon Icon { get; } = icon;
    public Color Color { get; } = color;

    /// <summary>
    /// Gets data needed to populate the content of the state column.
    /// </summary>
    internal static ResourceStateViewModel GetStateViewModel(ResourceViewModel resource, IStringLocalizer<Columns> loc)
    {
        // Browse the icon library at: https://aka.ms/fluentui-system-icons

        Icon icon;
        Color color;

        if (resource.IsStopped())
        {
            if (resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                icon = new Icons.Filled.Size16.ErrorCircle();
                color = Color.Error;
            }
            else if (resource.IsFinishedState())
            {
                // Process completed successfully.
                icon = new Icons.Regular.Size16.RecordStop();
                color = Color.Info;
            }
            else
            {
                // Process completed, which may not have been unexpected.
                icon = new Icons.Filled.Size16.Warning();
                color = Color.Warning;
            }
        }
        else if (resource.IsUnusableTransitoryState() || resource.IsUnknownState())
        {
            icon = new Icons.Filled.Size16.CircleHint(); // A dashed, hollow circle.
            color = Color.Info;
        }
        else if (resource.IsRuntimeUnhealthy())
        {
            icon = new Icons.Filled.Size16.Warning();
            color = Color.Warning;
        }
        else if (resource.HasNoState())
        {
            icon = new Icons.Filled.Size16.Circle();
            color = Color.Info;
        }
        else if (resource.HealthStatus is not HealthStatus.Healthy)
        {
            icon = new Icons.Filled.Size16.CheckmarkCircleWarning();
            color = Color.Warning;
        }
        else if (!string.IsNullOrEmpty(resource.StateStyle))
        {
            (icon, color) = resource.StateStyle switch
            {
                "warning" => ((Icon)new Icons.Filled.Size16.Warning(), Color.Warning),
                "error" => (new Icons.Filled.Size16.ErrorCircle(), Color.Error),
                "success" => (new Icons.Filled.Size16.CheckmarkCircle(), Color.Success),
                "info" => (new Icons.Filled.Size16.Info(), Color.Info),
                _ => (new Icons.Filled.Size16.Circle(), Color.Neutral)
            };
        }
        else
        {
            icon = new Icons.Filled.Size16.CheckmarkCircle();
            color = Color.Success;
        }

        var text = GetStateText(resource, loc);

        return new ResourceStateViewModel(text, icon, color);
    }

    /// <summary>
    /// Gets the tooltip for a cell in the state column of the resource grid.
    /// </summary>
    /// <remarks>
    /// This is a static method so it can be called at the level of the parent column.
    /// </remarks>
    internal static string GetResourceStateTooltip(ResourceViewModel resource, IStringLocalizer<Columns> loc)
    {
        if (resource.IsStopped())
        {
            if (resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                return loc.GetString(nameof(Columns.StateColumnResourceExitedUnexpectedly), resource.ResourceType, exitCode);
            }
            else
            {
                // Process completed, which may not have been unexpected.
                return loc.GetString(nameof(Columns.StateColumnResourceExited), resource.ResourceType);
            }
        }
        else if (resource is { KnownState: KnownResourceState.Running, HealthStatus: not HealthStatus.Healthy })
        {
            // Resource is running but not healthy (initializing).
            return loc[nameof(Columns.RunningAndUnhealthyResourceStateToolTip)];
        }
        else if (resource.IsRuntimeUnhealthy() && resource.IsContainer())
        {
            // DCP reports the container runtime is unhealthy. Most likely the container runtime (e.g. Docker) isn't running.
            return loc[nameof(Columns.StateColumnResourceContainerRuntimeUnhealthy)];
        }

        // Fallback to text displayed in column.
        return GetStateText(resource, loc);
    }

    private static string GetStateText(ResourceViewModel resource, IStringLocalizer<Columns> loc)
    {
        return resource switch
        {
            { State: null or "" } => loc[Columns.UnknownStateLabel],
            { KnownState: KnownResourceState.Running, HealthStatus: not HealthStatus.Healthy } => $"{resource.State.Humanize()} ({(resource.HealthStatus ?? HealthStatus.Unhealthy).Humanize()})",
            _ => resource.State.Humanize()
        };
    }
}
