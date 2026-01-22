// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tui;

/// <summary>
/// Component that displays running AppHosts with selection and scope indicators.
/// </summary>
internal sealed class AppHostsPanel : TuiComponent, IInputHandler
{
    private readonly IAuxiliaryBackchannelMonitor _monitor;

    public AppHostsPanel(IAuxiliaryBackchannelMonitor monitor, Dictionary<string, object?>? props = null)
        : base(props)
    {
        _monitor = monitor;
    }

    public bool HandleInput(ConsoleKeyInfo keyInfo)
    {
        // Handle number keys 1-9 for quick selection
        if (keyInfo.KeyChar >= '1' && keyInfo.KeyChar <= '9')
        {
            var index = keyInfo.KeyChar - '0';
            if (SelectByIndex(index))
            {
                return true;
            }
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                return SelectRelative(-1);
            case ConsoleKey.DownArrow:
                return SelectRelative(1);
        }

        return false;
    }

    public override IRenderable Render()
    {
        var connections = _monitor.Connections.Values.ToList();
        var selectedPath = _monitor.SelectedAppHostPath;

        var rows = new List<IRenderable>();

        if (connections.Count == 0)
        {
            rows.Add(new Markup("[dim]No AppHosts running[/]"));
            rows.Add(new Markup(""));
            rows.Add(new Markup("[dim]Use [cyan]aspire run[/] to start[/]"));
        }
        else
        {
            for (var i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                var appHostPath = connection.AppHostInfo?.AppHostPath;
                var name = GetDisplayName(appHostPath);

                var isSelected = IsSelected(connection, selectedPath);
                var indicator = isSelected ? "[green]●[/]" : "[dim]○[/]";
                var scopeIndicator = connection.IsInScope ? " [cyan]✓[/]" : "";
                var nameStyle = isSelected ? "[bold]" : "[dim]";

                var line = $"{indicator} [cyan]{i + 1}[/] {nameStyle}{name.EscapeMarkup()}[/]{scopeIndicator}";
                rows.Add(new Markup(line));
            }

            rows.Add(new Markup(""));
            rows.Add(new Markup("[dim]Use [cyan]1-9[/] or [cyan]↑/↓[/] to select[/]"));
            rows.Add(new Markup("[dim][cyan]✓[/] = in scope, /stop to stop[/]"));
        }

        var content = new Rows(rows);

        return new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.MediumPurple1),
            Header = new PanelHeader(" AppHosts ", Justify.Center),
            Padding = new Padding(1, 0),
            Width = 24
        };
    }

    /// <summary>
    /// Gets the number of running AppHosts.
    /// </summary>
    public int ConnectionCount => _monitor.Connections.Count;

    /// <summary>
    /// Gets the list of running AppHost connections for selection purposes.
    /// </summary>
    public IReadOnlyList<AppHostAuxiliaryBackchannel> GetConnections()
    {
        return _monitor.Connections.Values.ToList();
    }

    /// <summary>
    /// Gets the currently selected connection.
    /// </summary>
    public AppHostAuxiliaryBackchannel? SelectedConnection => _monitor.SelectedConnection;

    /// <summary>
    /// Selects an AppHost by its index (1-based).
    /// </summary>
    /// <param name="index">The 1-based index of the AppHost to select.</param>
    /// <returns>True if the selection was successful; otherwise, false.</returns>
    public bool SelectByIndex(int index)
    {
        var connections = _monitor.Connections.Values.ToList();
        if (index < 1 || index > connections.Count)
        {
            return false;
        }

        var connection = connections[index - 1];
        _monitor.SelectedAppHostPath = connection.AppHostInfo?.AppHostPath;
        ScheduleRerender();
        return true;
    }

    /// <summary>
    /// Selects an AppHost by name (partial match supported).
    /// </summary>
    /// <param name="name">The name or partial name of the AppHost to select.</param>
    /// <returns>True if the selection was successful; otherwise, false.</returns>
    public bool SelectByName(string name)
    {
        var connections = _monitor.Connections.Values.ToList();
        var match = connections.FirstOrDefault(c =>
            GetDisplayName(c.AppHostInfo?.AppHostPath)
                .Contains(name, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return false;
        }

        _monitor.SelectedAppHostPath = match.AppHostInfo?.AppHostPath;
        ScheduleRerender();
        return true;
    }

    private bool SelectRelative(int delta)
    {
        var connections = _monitor.Connections.Values.ToList();
        if (connections.Count == 0)
        {
            return false;
        }

        var currentIndex = GetSelectedIndex(connections);
        var nextIndex = (currentIndex + delta) % connections.Count;
        if (nextIndex < 0)
        {
            nextIndex += connections.Count;
        }

        _monitor.SelectedAppHostPath = connections[nextIndex].AppHostInfo?.AppHostPath;
        ScheduleRerender();
        return true;
    }

    private int GetSelectedIndex(IReadOnlyList<AppHostAuxiliaryBackchannel> connections)
    {
        if (_monitor.SelectedConnection is not null)
        {
            for (var i = 0; i < connections.Count; i++)
            {
                if (ReferenceEquals(connections[i], _monitor.SelectedConnection))
                {
                    return i;
                }
            }
        }

        if (!string.IsNullOrEmpty(_monitor.SelectedAppHostPath))
        {
            for (var i = 0; i < connections.Count; i++)
            {
                var appHostPath = connections[i].AppHostInfo?.AppHostPath;
                if (!string.IsNullOrEmpty(appHostPath) &&
                    string.Equals(Path.GetFullPath(appHostPath), Path.GetFullPath(_monitor.SelectedAppHostPath), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets autocomplete suggestions for AppHost names.
    /// </summary>
    /// <param name="partial">The partial input to match.</param>
    /// <returns>A list of matching AppHost names.</returns>
    public IReadOnlyList<string> GetCompletions(string partial)
    {
        var connections = _monitor.Connections.Values.ToList();
        return connections
            .Select(c => GetDisplayName(c.AppHostInfo?.AppHostPath))
            .Where(name => name.Contains(partial, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private bool IsSelected(AppHostAuxiliaryBackchannel connection, string? selectedPath)
    {
        if (string.IsNullOrEmpty(selectedPath))
        {
            // If no explicit selection, check if this is the auto-selected one
            return ReferenceEquals(connection, _monitor.SelectedConnection);
        }

        var appHostPath = connection.AppHostInfo?.AppHostPath;
        if (string.IsNullOrEmpty(appHostPath))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(appHostPath),
            Path.GetFullPath(selectedPath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDisplayName(string? appHostPath)
    {
        if (string.IsNullOrEmpty(appHostPath))
        {
            return "(unknown)";
        }

        // Get the project name from the path (e.g., "MyApp.AppHost" from "/path/to/MyApp.AppHost/MyApp.AppHost.csproj")
        var directory = Path.GetDirectoryName(appHostPath);
        if (!string.IsNullOrEmpty(directory))
        {
            var dirName = Path.GetFileName(directory);
            if (!string.IsNullOrEmpty(dirName))
            {
                // Remove .AppHost suffix for cleaner display
                if (dirName.EndsWith(".AppHost", StringComparison.OrdinalIgnoreCase))
                {
                    return dirName[..^8];
                }
                return dirName;
            }
        }

        return Path.GetFileNameWithoutExtension(appHostPath);
    }
}
