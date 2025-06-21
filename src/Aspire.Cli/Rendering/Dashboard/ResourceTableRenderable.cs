// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class ResourceTableRenderable(DashboardState state) : JustInTimeRenderable
{
    protected override IRenderable Build()
    {
        var table = new Table().Border(TableBorder.Rounded);

        // Add columns
        table.AddColumn(RunCommandStrings.Resource);
        table.AddColumn(RunCommandStrings.Type);
        table.AddColumn(RunCommandStrings.State);
        table.AddColumn(RunCommandStrings.Endpoints);

        if (state.ResourceStates.Count == 0)
        {
                var placeholders = new Markup[table.Columns.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    placeholders[i] = new Markup("--");
                }
                table.Rows.Add(placeholders);
        }

        foreach (var resourceStates in state.ResourceStates)
            {
                var nameRenderable = new Text(resourceStates.Key, new Style().Foreground(Color.White));

                var typeRenderable = new Text(resourceStates.Value.Type, new Style().Foreground(Color.White));

                var stateRenderable = (resourceStates.Value.State, resourceStates.Value.Health) switch
                {
                    // Map the combination of running states and health. If it is running but degrated or unhealthy
                    // then we substitute in the health state (since we combine the health and state together).
                    ("Running", "Healthy") => new Text(resourceStates.Value.State, new Style().Foreground(Color.Green)),
                    ("Running", "Unhealthy") => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Red)),
                    ("Running", "Degraded") => new Text(resourceStates.Value.Health, new Style().Foreground(Color.Yellow)),

                    ("Starting", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.LightGreen)),
                    ("FailedToStart", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.Red)),
                    ("Waiting", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.White)),

                    ("Exited", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),
                    ("Finished", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),
                    ("NotStarted", _) => new Text(resourceStates.Value.State, new Style().Foreground(Color.Grey)),

                    _ => new Text(resourceStates.Value.State ?? "Unknown", new Style().Foreground(Color.Grey))
                };

                IRenderable endpointsRenderable = new Text(TemplatingStrings.None);
                if (resourceStates.Value.Endpoints?.Length > 0)
                {
                    endpointsRenderable = new Rows(
                        resourceStates.Value.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                    );
                }

                table.AddRow(nameRenderable, typeRenderable, stateRenderable, endpointsRenderable);
            }

        table.Expand();

        return table;
    }
}