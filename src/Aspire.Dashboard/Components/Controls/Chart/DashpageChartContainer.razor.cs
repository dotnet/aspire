// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components;

public partial class DashpageChartContainer
{
    private readonly string _filterButtonId = $"filter-button-{Guid.NewGuid():N}";
    private bool _filterPopupOpen;
}

