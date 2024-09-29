// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Extensions;

internal static class ComponentExtensions
{
    public static async Task SafeRefreshDataAsync<T>(this FluentDataGrid<T>? dataGrid)
    {
        if (dataGrid != null)
        {
            await dataGrid.RefreshDataAsync().ConfigureAwait(false);
        }
    }

    public static async Task ExecuteOnDefault<T>(this FluentDataGridRow<T> row, Func<T, Task> call)
    {
        // Don't trigger on header rows.
        if (row.RowType == DataGridRowType.Default)
        {
            await call(row.Item!).ConfigureAwait(false);
        }
    }

    public static void ExecuteOnDefault<T>(this FluentDataGridRow<T> row, Action<T> call)
    {
        // Don't trigger on header rows.
        if (row.RowType == DataGridRowType.Default)
        {
            call(row.Item!);
        }
    }
}
