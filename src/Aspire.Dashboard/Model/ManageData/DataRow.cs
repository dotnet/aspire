// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model.ManageData;

/// <summary>
/// Represents a nested data row within a resource row.
/// </summary>
public sealed class DataRow
{
    public required string DisplayName { get; init; }

    /// <summary>
    /// The type of data this row represents.
    /// </summary>
    public required AspireDataType DataType { get; init; }

    /// <summary>
    /// The total count of items (logs, traces, or metrics). Null if count is not available.
    /// </summary>
    public int? DataCount { get; init; }

    /// <summary>
    /// The icon representing this data type.
    /// </summary>
    public required Icon Icon { get; init; }

    /// <summary>
    /// The URL to navigate to when clicking on this data row.
    /// </summary>
    public required string Url { get; init; }

    public bool Selected { get; set; }
}
