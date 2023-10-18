// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Model;

public class CounterChartViewModel
{
    public List<DimensionFilterViewModel> DimensionFilters { get; } = new();
}

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class DimensionFilterViewModel
{
    public required string Name { get; init; }
    public List<DimensionValueViewModel> Values { get; } = new();
    public IEnumerable<DimensionValueViewModel> SelectedValues { get; set; } = Array.Empty<DimensionValueViewModel>();
    public bool PopupVisible { get; set; }

    public Task OnSearchAsync(OptionsSearchEventArgs<DimensionValueViewModel> e)
    {
        e.Items = Values.Where(i => i.Name.StartsWith(e.Text, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.Name);
        return Task.CompletedTask;
    }

    private string DebuggerToString() => $"Name = {Name}, SelectedValues = {SelectedValues.Count()}";
}

[DebuggerDisplay("Name = {Name}, Empty = {Empty}")]
public class DimensionValueViewModel
{
    public required string Name { get; init; }
    public bool Empty { get; init; }
}

