// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Utils;

// Temporary work around to set MaxItemCount on Virtualize component via reflection.
// Required because dashboard currently targets .NET 8 and MaxItemCount isn't available.
//
// ASP.NET Core issue: https://github.com/dotnet/aspnetcore/issues/63651
// Note that this work around should be left in place for a while after the issue is fixed in ASP.NET Core.
// .NET 9 needs to be patched, and users may have unpatched versions of .NET 9 on their machines for a while.
public static class VirtualizeHelper<TItem>
{
    private static readonly Func<Virtualize<TItem>, int>? s_getMaxItemCount =
        CreateGetter();

    private static readonly Action<Virtualize<TItem>, int>? s_setMaxItemCount =
        CreateSetter();

    private static Func<Virtualize<TItem>, int>? CreateGetter()
    {
        var type = typeof(Virtualize<TItem>);
        var prop = type.GetProperty("MaxItemCount", BindingFlags.Instance | BindingFlags.Public);

        if (prop == null || !prop.CanRead)
        {
            return null;
        }

        var instance = Expression.Parameter(type, "virtualize");
        var body = Expression.Property(instance, prop);

        return Expression.Lambda<Func<Virtualize<TItem>, int>>(body, instance).Compile();
    }

    private static Action<Virtualize<TItem>, int>? CreateSetter()
    {
        var type = typeof(Virtualize<TItem>);
        var prop = type.GetProperty("MaxItemCount", BindingFlags.Instance | BindingFlags.Public);

        if (prop == null || !prop.CanWrite)
        {
            return null;
        }

        var instance = Expression.Parameter(type, "virtualize");
        var valueParam = Expression.Parameter(typeof(int), "value");
        var body = Expression.Assign(Expression.Property(instance, prop), valueParam);

        return Expression.Lambda<Action<Virtualize<TItem>, int>>(body, instance, valueParam).Compile();
    }

    public static bool TrySetMaxItemCount(Virtualize<TItem> virtualize, int max)
    {
        if (s_getMaxItemCount == null || s_setMaxItemCount == null)
        {
            return false;
        }

        if (s_getMaxItemCount(virtualize) == max)
        {
            return false;
        }

        s_setMaxItemCount(virtualize, max);
        return true;
    }
}

public static class FluentDataGridHelper<TGridItem>
{
    private static readonly Func<FluentDataGrid<TGridItem>, Virtualize<(int, TGridItem)>>? s_getVirtualize =
        CreateGetter();

    private static Func<FluentDataGrid<TGridItem>, Virtualize<(int, TGridItem)>>? CreateGetter()
    {
        var type = typeof(FluentDataGrid<TGridItem>);
        var field = type.GetField("_virtualizeComponent", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            return null;
        }

        var instance = Expression.Parameter(type, "dataGrid");
        var body = Expression.Convert(Expression.Field(instance, field), typeof(Virtualize<(int, TGridItem)>));

        return Expression.Lambda<Func<FluentDataGrid<TGridItem>, Virtualize<(int, TGridItem)>>> (body, instance).Compile();
    }

    private static Virtualize<(int, TGridItem)>? GetVirtualize(FluentDataGrid<TGridItem> dataGrid)
        => s_getVirtualize?.Invoke(dataGrid);

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
