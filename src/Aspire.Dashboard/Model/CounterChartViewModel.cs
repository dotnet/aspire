// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.FluentUI.AspNetCore.Components;

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
    public HashSet<DimensionValueViewModel> SelectedValues { get; } = new();
    public bool PopupVisible { get; set; }

    public Task OnSearchAsync(OptionsSearchEventArgs<DimensionValueViewModel> e)
    {
        e.Items = Values.Where(i => i.Name.StartsWith(e.Text, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.Name);
        return Task.CompletedTask;
    }

    public bool? AreAllTypesVisible
    {
        get
        {
            return SelectedValues.SetEquals(Values)
                ? true
                : SelectedValues.Count == 0
                    ? false
                    : null;
        }
        set
        {
            if (value is true)
            {
                SelectedValues.UnionWith(Values);
            }
            else if (value is false)
            {
                SelectedValues.Clear();
            }
        }
    }

    private string DebuggerToString() => $"Name = {Name}, SelectedValues = {SelectedValues.Count}";
}

[DebuggerDisplay("Name = {Name}, Empty = {Empty}")]
public class DimensionValueViewModel
{
    public required string Name { get; init; }
    public bool Empty { get; init; }
}

