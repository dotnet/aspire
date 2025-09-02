// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Controls;

public partial class TextVisualizer : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _containerElement;
    private bool _isObserving;

    [Inject]
    public required ThemeManager ThemeManager { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Parameter]
    public required TextVisualizerViewModel ViewModel { get; set; }

    [Parameter]
    public bool HideLineNumbers { get; set; }

    [Parameter]
    public bool DisplayUnformatted { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ThemeManager.EnsureInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Controls/TextVisualizer.razor.js");
        }

        if (_jsModule is not null)
        {
            if (ViewModel.FormatKind is not DashboardUIHelpers.PlaintextFormat)
            {
                if (!_isObserving)
                {
                    _isObserving = true;
                    await _jsModule.InvokeVoidAsync("connectObserver", _containerElement);
                }
            }
            else
            {
                if (_isObserving)
                {
                    _isObserving = false;
                    await _jsModule.InvokeVoidAsync("disconnectObserver", _containerElement);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                if (_isObserving)
                {
                    _isObserving = false;
                    await _jsModule.InvokeVoidAsync("disconnectObserver", _containerElement);
                }
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Per https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-7.0#javascript-interop-calls-without-a-circuit
                // this is one of the calls that will fail if the circuit is disconnected, and we just need to catch the exception so it doesn't pollute the logs
            }
        }
    }

    private string GetLogContentClass()
    {
        // we support light (a11y-light-min) and dark (a11y-dark-min) themes.
        // syntax to force a theme for highlight.js is "theme-{themeName}"
        return $"log-content highlight-line language-{ViewModel.FormatKind} theme-a11y-{ThemeManager.EffectiveTheme.ToLower()}-min";
    }
}
