// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Resources;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class GridValue
{
    [Parameter, EditorRequired]
    public string? Value { get; set; }

    [Parameter, EditorRequired]
    public required string ValueDescription { get; set; }

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
    public int? MaxDisplayLength { get; set; }

    [Parameter]
    public string? ToolTip { get; set; }

    [Parameter]
    public string PreCopyToolTip { get; set; } = null!;

    [Parameter]
    public string PostCopyToolTip { get; set; } = null!;

    [Inject]
    public required IDialogService DialogService { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; init; }

    private readonly Icon _maskIcon = new Icons.Regular.Size16.EyeOff();
    private readonly Icon _unmaskIcon = new Icons.Regular.Size16.Eye();
    private readonly string _copyId = $"copy-{Guid.NewGuid():N}";
    private readonly string _menuAnchorId = $"menu-{Guid.NewGuid():N}";
    private readonly string _openVisualizerId = $"open-visualizer-{Guid.NewGuid():N}";
    private bool _isMenuOpen;
    private IJSObjectReference? _onClickHandler;
    private DotNetObjectReference<GridValue>? _thisReference;

    protected override void OnInitialized()
    {
        PreCopyToolTip = Loc[nameof(ControlsStrings.GridValueCopyToClipboard)];
        PostCopyToolTip = Loc[nameof(ControlsStrings.GridValueCopied)];
    }

    private async Task OnOpenChangedAsync()
    {
        if (_isMenuOpen)
        {
            _thisReference?.Dispose();

            try
            {
                await JS.InvokeVoidAsync("window.unregisterGlobalKeydownListener", _openVisualizerId, _onClickHandler);
            }
            catch (JSDisconnectedException)
            {
                // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
                // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
            }

            await JSInteropHelpers.SafeDisposeAsync(_onClickHandler);
        }
        else
        {
            _thisReference = DotNetObjectReference.Create(this);
            _onClickHandler = await JS.InvokeAsync<IJSObjectReference>("window.registerOpenTextVisualizerOnClick", _openVisualizerId, _thisReference);
        }
    }

    private string GetContainerClass() => EnableMasking ? "container masking-enabled" : "container";

    private async Task ToggleMaskStateAsync()
        => await IsMaskedChanged.InvokeAsync(!IsMasked);

    private string TrimLength(string? text)
    {
        if (text is not null && MaxDisplayLength is int maxLength && text.Length > maxLength)
        {
            return text[..maxLength];
        }

        return text ?? "";
    }

    private void ToggleMenuOpen()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    [JSInvokable]
    public async Task OpenTextVisualizerAsync()
    {
        var parameters = new DialogParameters
        {
            Title = ValueDescription,
            PrimaryActionEnabled = false,
            SecondaryActionEnabled = false,
            Width = ViewportInformation.IsDesktop ? "60vw" : "100vw",
            Height = ViewportInformation.IsDesktop ? "60vh" : "100vh",
            TrapFocus = true,
            Modal = true,
            PreventScroll = true
        };

        await DialogService.ShowDialogAsync<TextVisualizerDialog>(new TextVisualizerDialogViewModel(Value!), parameters);
    }
}
