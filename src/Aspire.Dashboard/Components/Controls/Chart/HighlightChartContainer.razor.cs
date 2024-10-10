// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components;

public partial class HighlightChartContainer
{
    private static int s_nextFilterButtonId;
    private readonly string _filterButtonId = $"filter-button-{Interlocked.Increment(ref s_nextFilterButtonId)}";
    private bool _filterPopupOpen;
}

