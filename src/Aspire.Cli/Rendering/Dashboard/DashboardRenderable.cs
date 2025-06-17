// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class DashboardRenderable(DashboardState state) : JustInTimeRenderable
{
    protected override bool HasDirtyChildren()
    {
        return true;
    }

    protected override IRenderable Build()
    {
        return BuildResourcesTable();
    }

    private IRenderable BuildResourcesTable()
    {
        var table = new Table().Border(TableBorder.Rounded);

        // Add columns
        table.AddColumn(RunCommandStrings.Resource);
        table.AddColumn(RunCommandStrings.Type);
        table.AddColumn(RunCommandStrings.State);
        table.AddColumn(RunCommandStrings.Health);
        table.AddColumn(RunCommandStrings.Endpoints);

        foreach (var resourceStates in state.ResourceStates)
        {
            var nameRenderable = new Text(resourceStates.Key, new Style().Foreground(Color.White));

            var typeRenderable = new Text(resourceStates.Value.Type, new Style().Foreground(Color.White));

            var stateRenderable = resourceStates.Value.State switch
            {
                "Running" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Green)),
                "Starting" => new Text(resourceStates.Value.State, new Style().Foreground(Color.LightGreen)),
                "FailedToStart" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Red)),
                "Waiting" => new Text(resourceStates.Value.State, new Style().Foreground(Color.White)),
                "Unhealthy" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Yellow)),
                "Exited" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),
                "Finished" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),
                "NotStarted" => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),
                _ => new Text(resourceStates.Value.State ?? "Unknown", new Style().Foreground(Color.Grey))
            };

            var healthRenderable = resourceStates.Value.Health switch
            {
                "Healthy" => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Green)),
                "Degraded" => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Yellow)),
                "Unhealthy" => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Red)),
                null => new Text(TemplatingStrings.Unknown, new Style().Foreground(Color.Grey)),
                _ => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Grey))
            };

            IRenderable endpointsRenderable = new Text(TemplatingStrings.None);
            if (resourceStates.Value.Endpoints?.Length > 0)
            {
                endpointsRenderable = new Rows(
                    resourceStates.Value.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                );
            }

            table.AddRow(nameRenderable, typeRenderable, stateRenderable, healthRenderable, endpointsRenderable);
        }

        return table;
    }
}