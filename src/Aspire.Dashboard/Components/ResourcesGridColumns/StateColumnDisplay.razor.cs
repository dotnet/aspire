// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.ResourcesGridColumns;

public partial class StateColumnDisplay
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter, EditorRequired]
    public required Dictionary<ApplicationKey, int>? UnviewedErrorCounts { get; set; }

    [Inject]
    public required IStringLocalizer<Columns> Loc { get; init; }

    internal static string? GetResourceStateTooltip(ResourceViewModel resource, IStringLocalizer<Columns> loc)
    {
        return GetResourceStateTooltip(
            resource,
            loc[Columns.StateColumnResourceExitedUnexpectedly].Value,
            loc[Columns.StateColumnResourceExited].Value,
            loc[nameof(Columns.RunningAndUnhealthyResourceStateToolTip)]);
    }

    /// <summary>
    /// Gets the tooltip for a cell in the state column of the resource grid.
    /// </summary>
    /// <remarks>
    /// This is a static method so it can be called at the level of the parent column.
    /// </remarks>
    internal static string? GetResourceStateTooltip(ResourceViewModel resource, string exitedUnexpectedlyTooltip, string exitedTooltip, string runningAndUnhealthyTooltip)
    {
        if (resource.IsStopped())
        {
            if (resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                return string.Format(CultureInfo.CurrentCulture, exitedUnexpectedlyTooltip, resource.ResourceType, exitCode);
            }
            else
            {
                // Process completed, which may not have been unexpected.
                return string.Format(CultureInfo.CurrentCulture, exitedTooltip, resource.ResourceType);
            }
        }
        else if (resource is { KnownState: KnownResourceState.Running, HealthStatus: not HealthStatus.Healthy and not null })
        {
            // Resource is running but not healthy (initializing).
            return runningAndUnhealthyTooltip;
        }

        return null;
    }

    /// <summary>
    /// Gets data needed to populate the content of the state column.
    /// </summary>
    internal static ResourceStateViewModel GetStateViewModel(ResourceViewModel resource, string unknownStateLabel)
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
                icon = new Icons.Filled.Size16.CheckmarkUnderlineCircle();
                color = Color.Success;
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
        else if (resource.HasNoState())
        {
            icon = new Icons.Filled.Size16.Circle();
            color = Color.Neutral;
        }
        else if (resource.HealthStatus is not HealthStatus.Healthy and not null)
        {
            icon = new Icons.Filled.Size16.CheckmarkCircleWarning();
            color = Color.Neutral;
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

        var text = resource switch
        {
            { State: null or "" } => unknownStateLabel,
            { KnownState: KnownResourceState.Running, HealthStatus: not HealthStatus.Healthy and not null } => $"{resource.State.Humanize()} ({(resource.HealthStatus ?? HealthStatus.Unhealthy).Humanize()})",
            _ => resource.State.Humanize()
        };

        return new ResourceStateViewModel(text, icon, color);
    }

    internal record class ResourceStateViewModel(string Text, Icon Icon, Color Color);
}
