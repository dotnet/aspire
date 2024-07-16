// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ChartFilters
{
    [Inject]
    public required CurrentChartViewModel ChartViewModel { get; init; }

    protected override void OnInitialized()
    {
        InstrumentViewModel.DataUpdateSubscriptions.Add(() =>
        {
            ChartViewModel.ShowCounts = InstrumentViewModel.ShowCount;
            return Task.CompletedTask;
        });
    }

    private void ShowCountChanged()
    {
        InstrumentViewModel.ShowCount = ChartViewModel.ShowCounts;
    }
}
