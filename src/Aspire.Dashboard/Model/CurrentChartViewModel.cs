// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class CurrentChartViewModel
{
    public bool OnlyShowValueChangesInTable { get; set; } = true;
    public bool ShowCounts { get; set; }
    public List<DimensionFilterViewModel> DimensionFilters { get; } = [];
    public string? PreviousMeterName { get; set; }
    public string? PreviousInstrumentName { get; set; }

}
