// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Layout;

public partial class DesktopNavMenu : ComponentBase
{
    internal static Icon ResourcesIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.AppFolder()
                  : new Icons.Regular.Size24.AppFolder();

    internal static Icon ConsoleLogsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.SlideText()
                  : new Icons.Regular.Size24.SlideText();

    internal static Icon StructuredLogsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.SlideTextSparkle()
                  : new Icons.Regular.Size24.SlideTextSparkle();

    internal static Icon TracesIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.GanttChart()
                  : new Icons.Regular.Size24.GanttChart();

    internal static Icon MetricsIcon(bool active = false) =>
        active ? new Icons.Filled.Size24.ChartMultiple()
                  : new Icons.Regular.Size24.ChartMultiple();
}
