// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

// Temporary work around to set MaxItemCount on Virtualize component via reflection.
// Required because dashboard currently targets .NET 8 and MaxItemCount isn't available.
public static class VirtualizeHelper<TItem>
{
    private static readonly PropertyInfo? s_setMaxItemCountPropertyInfo = GetSetMaxItemCountMethodInfo();

    private static PropertyInfo? GetSetMaxItemCountMethodInfo()
    {
        var type = typeof(Virtualize<TItem>);
        return type.GetProperty("MaxItemCount", BindingFlags.Instance | BindingFlags.Public);
    }

    public static bool TrySetMaxItemCount(Virtualize<TItem> virtualize, int max)
    {
        if (s_setMaxItemCountPropertyInfo == null)
        {
            return false;
        }

        var currentMaxItemCount = (int)s_setMaxItemCountPropertyInfo.GetValue(virtualize)!;
        if (currentMaxItemCount == max)
        {
            return false;
        }

        s_setMaxItemCountPropertyInfo.SetValue(virtualize, max);
        return true;
    }
}

public static class FluentDataGridHelper<TGridItem>
{
    private static readonly FieldInfo? s_virtualizeComponentFieldInfo = GetVirtualizeComponentFieldInfo();

    private static FieldInfo? GetVirtualizeComponentFieldInfo()
    {
        var type = typeof(FluentDataGrid<TGridItem>);
        var field = type.GetField("_virtualizeComponent", BindingFlags.Instance | BindingFlags.NonPublic);

        return field;
    }

    private static Virtualize<(int, TGridItem)>? GetVirtualize(FluentDataGrid<TGridItem> dataGrid)
    {
        return s_virtualizeComponentFieldInfo?.GetValue(dataGrid) as Virtualize<(int, TGridItem)>;
    }

    public static bool TrySetMaxItemCount(FluentDataGrid<TGridItem> dataGrid, int max)
    {
        var virtualize = GetVirtualize(dataGrid);
        if (virtualize == null)
        {
            return false;
        }
        return VirtualizeHelper<(int, TGridItem)>.TrySetMaxItemCount(virtualize, max);
    }
}

