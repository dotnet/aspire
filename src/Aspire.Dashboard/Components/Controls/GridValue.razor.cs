// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class GridValue
{
    [Parameter, EditorRequired]
    public string? Value { get; set; }

    [Parameter, EditorRequired]
    public required string ValueDescription { get; set; }

    [Parameter]
    public string? TextVisualizerTitle { get; set; }

    /// <summary>
    /// Content to include, if any, before the Value string
    /// </summary>
    [Parameter]
    public RenderFragment? ContentBeforeValue { get; set; }

    /// <summary>
    /// Content to include, if any, after the Value string
    /// </summary>
    [Parameter]
    public RenderFragment? ContentAfterValue { get; set; }

    /// <summary>
    /// If set, copies this value instead of <see cref="Value"/>.
    /// </summary>
    [Parameter]
    public string? ValueToCopy { get; set; }

    /// <summary>
    /// If set, this value is visualized rather than <see cref="Value"/>.
    /// </summary>
    [Parameter]
    public string? ValueToVisualize { get; set; }

    /// <summary>
    /// Determines whether or not masking support is enabled for this value
    /// </summary>
    [Parameter]
    public bool EnableMasking { get; set; }

    /// <summary>
    /// Determines whether or not the value should currently be masked
    /// </summary>
    [Parameter]
    public bool IsMasked { get; set; }

    [Parameter]
    public bool EnableHighlighting { get; set; } = false;

    /// <summary>
    /// The text to highlight within the value when the value is displayed unmasked
    /// </summary>
    [Parameter]
    public string? HighlightText { get; set; }

    [Parameter]
    public EventCallback<bool> IsMaskedChanged { get; set; }

    [Parameter]
    public string? ToolTip { get; set; }

    [Parameter]
    public string PreCopyToolTip { get; set; } = null!;

    [Parameter]
    public string PostCopyToolTip { get; set; } = null!;

    private readonly Icon _maskIcon = new Icons.Regular.Size16.EyeOff();
    private readonly Icon _unmaskIcon = new Icons.Regular.Size16.Eye();
    private readonly string _copyId = $"copy-{Guid.NewGuid():N}";
    private readonly string _menuAnchorId = $"menu-{Guid.NewGuid():N}";
    private bool _isMenuOpen;

    protected override void OnInitialized()
    {
        PreCopyToolTip = Loc[nameof(ControlsStrings.GridValueCopyToClipboard)];
        PostCopyToolTip = Loc[nameof(ControlsStrings.GridValueCopied)];
    }

    private string GetContainerClass() => EnableMasking ? "container masking-enabled" : "container";

    private async Task ToggleMaskStateAsync()
    {
        IsMasked = !IsMasked;

        await IsMaskedChanged.InvokeAsync(IsMasked);
    }

    private void ToggleMenuOpen()
    {
        _isMenuOpen = !_isMenuOpen;
    }
}
