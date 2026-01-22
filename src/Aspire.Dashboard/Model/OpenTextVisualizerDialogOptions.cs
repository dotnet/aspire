// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Options for opening the text visualizer dialog.
/// </summary>
public sealed class OpenTextVisualizerDialogOptions
{
    public required ViewportInformation ViewportInformation { get; init; }
    public required IDialogService DialogService { get; init; }
    public required IStringLocalizer<Resources.Dialogs> DialogsLoc { get; init; }
    public required string ValueDescription { get; init; }
    public required string Value { get; init; }
    public bool ContainsSecret { get; init; }
    public string? DownloadFileName { get; init; }
}
