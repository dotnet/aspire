// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class PagedResult<T>
{
    public static readonly PagedResult<T> Empty = new()
    {
        TotalItemCount = 0,
        Items = new List<T>(),
        IsFull = false
    };

    public required int TotalItemCount { get; init; }
    public required List<T> Items { get; init; }
    public required bool IsFull { get; init; }
}
