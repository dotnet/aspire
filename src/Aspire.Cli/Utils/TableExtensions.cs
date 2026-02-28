// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Extension methods for Spectre.Console <see cref="Table"/>.
/// </summary>
internal static class TableExtensions
{
    /// <summary>
    /// Adds a column with a bold header to the table.
    /// </summary>
    public static Table AddBoldColumn(this Table table, string header)
    {
        table.AddColumn($"[bold]{header.EscapeMarkup()}[/]");
        return table;
    }

    /// <summary>
    /// Adds a column with a bold, non-wrapping header to the table.
    /// </summary>
    public static Table AddBoldColumn(this Table table, string header, bool noWrap)
    {
        var column = new TableColumn($"[bold]{header.EscapeMarkup()}[/]");

        if (noWrap)
        {
            column.NoWrap();
        }

        table.AddColumn(column);
        return table;
    }

    /// <summary>
    /// Adds a column with a bold header and a fixed width to the table.
    /// </summary>
    public static Table AddBoldColumn(this Table table, string header, int width)
    {
        var column = new TableColumn($"[bold]{header.EscapeMarkup()}[/]");
        column.Width = width;
        table.AddColumn(column);
        return table;
    }
}
