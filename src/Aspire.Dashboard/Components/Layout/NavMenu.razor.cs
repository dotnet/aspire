// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class NavMenu : ComponentBase
{
    private static Icon ResourcesIcon(bool secondary = false) =>
        secondary ? new Icons.Filled.Size24.AppFolder()
                  : new Icons.Regular.Size24.AppFolder();

    private static Icon ConsoleLogsIcon(bool secondary = false) =>
        secondary ? new Icons.Filled.Size24.SlideText()
                  : new Icons.Regular.Size24.SlideText();

    private static Icon StructuredLogsIcon(bool secondary = false) =>
        secondary ? new Icons.Filled.Size24.SlideTextSparkle()
                  : new Icons.Regular.Size24.SlideTextSparkle();

    private static Icon TracesIcon(bool secondary = false) =>
        secondary ? new Icons.Filled.Size24.GanttChart()
                  : new Icons.Regular.Size24.GanttChart();

    private static Icon MetricsIcon(bool secondary = false) =>
        secondary ? new Icons.Filled.Size24.ChartMultiple()
                  : new Icons.Regular.Size24.ChartMultiple();
}
