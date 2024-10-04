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

    /// <summary>
    /// Gets the tooltip for a cell in the state column of the resource grid.
    /// </summary>
    /// <remarks>
    /// This is a static method so it can be called at the level of the parent column.
    /// </remarks>
    public static string? GetResourceStateTooltip(ResourceViewModel resource, IStringLocalizer<Columns> Loc)
    {
        if (resource.IsStopped())
        {
            if (resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.StateColumnResourceExitedUnexpectedly], resource.ResourceType, exitCode);
            }
            else
            {
                // Process completed, which may not have been unexpected.
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.StateColumnResourceExited], resource.ResourceType);
            }
        }
        else if (resource.KnownState is KnownResourceState.Running && resource.HealthStatus is not HealthStatus.Healthy)
        {
            // Resource is running but not healthy (initializing).
            return Loc[nameof(Columns.RunningAndUnhealthyResourceStateToolTip)];
        }

        return null;
    }

    /// <summary>
    /// Gets data needed to populate the content of the state column.
    /// </summary>
    private ResourceStateViewModel GetStateViewModel()
    {
        // Browse the icon library at: https://aka.ms/fluentui-system-icons

        Icon icon;
        Color color;

        if (Resource.IsStopped())
        {
            if (Resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                icon = new Icons.Filled.Size16.ErrorCircle();
                color = Color.Error;
            }
            else if (Resource.IsFinishedState())
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
        else if (Resource.IsUnusableTransitoryState() || Resource.IsUnknownState())
        {
            icon = new Icons.Filled.Size16.CircleHint(); // A dashed, hollow circle.
            color = Color.Info;
        }
        else if (Resource.HasNoState())
        {
            icon = new Icons.Filled.Size16.Circle();
            color = Color.Neutral;
        }
        else if (Resource.HealthStatus is not HealthStatus.Healthy)
        {
            icon = new Icons.Filled.Size16.CheckmarkCircleWarning();
            color = Color.Neutral;
        }
        else if (!string.IsNullOrEmpty(Resource.StateStyle))
        {
            (icon, color) = Resource.StateStyle switch
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

        var text = Resource switch
        {
            { State: null or "" } => Loc[Columns.UnknownStateLabel],
            { KnownState: KnownResourceState.Running, HealthStatus: not HealthStatus.Healthy } => $"{Resource.State.Humanize()} ({(Resource.HealthStatus ?? HealthStatus.Unhealthy).Humanize()})",
            _ => Resource.State.Humanize()
        };

        return new ResourceStateViewModel(text, icon, color);
    }

    private record class ResourceStateViewModel(string Text, Icon Icon, Color Color);
}
