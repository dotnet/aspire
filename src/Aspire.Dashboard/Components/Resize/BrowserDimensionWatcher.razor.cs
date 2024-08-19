// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Resize;

public class BrowserDimensionWatcher : ComponentBase
{
    // Set our mobile cutoff at 768 pixels, which is ~medium tablet size
    private const int MobileCutoffPixelWidth = 768;

    // A small enough height that the filters button takes up too much of the vertical space on screen
    private const int LowHeightCutoffPixelWidth = 400;

    // Very close to the minimum width we need to support (320px)
    private const int LowWidthCutoffPixelWidth = 350;

    [Parameter]
    public ViewportInformation? ViewportInformation { get; set; }

    [Parameter]
    public EventCallback<ViewportInformation?> ViewportInformationChanged { get; set; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    [Inject]
    public required DimensionManager DimensionManager { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var viewport = await JS.InvokeAsync<ViewportSize>("window.getWindowDimensions");
            ViewportInformation = GetViewportInformation(viewport);
            DimensionManager.InvokeOnBrowserDimensionsChanged(ViewportInformation);
            await ViewportInformationChanged.InvokeAsync(ViewportInformation);

            await JS.InvokeVoidAsync("window.listenToWindowResize", DotNetObjectReference.Create(this));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public async Task OnResizeAsync(ViewportSize viewportSize)
    {
        var newViewportInformation = GetViewportInformation(viewportSize);

        if (newViewportInformation.IsDesktop != ViewportInformation!.IsDesktop
            || newViewportInformation.IsUltraLowHeight != ViewportInformation.IsUltraLowHeight
            || newViewportInformation.IsUltraLowWidth != ViewportInformation.IsUltraLowWidth)
        {
            ViewportInformation = newViewportInformation;
            DimensionManager.IsResizing = true;
            // A re-render happens on components after ViewportInformationChanged is invoked
            // we should invoke OnBrowserDimensionsChanged first so that listeners of it
            // that are outside of the UI tree have the current viewport kind internally when components
            // call them
            DimensionManager.InvokeOnBrowserDimensionsChanged(newViewportInformation);
            await ViewportInformationChanged.InvokeAsync(newViewportInformation);
            DimensionManager.IsResizing = false;
        }
    }

    private static ViewportInformation GetViewportInformation(ViewportSize viewportSize)
    {
        return new ViewportInformation(
            IsDesktop: viewportSize.Width > MobileCutoffPixelWidth,
            IsUltraLowHeight: viewportSize.Height < LowHeightCutoffPixelWidth,
            IsUltraLowWidth: viewportSize.Width < LowWidthCutoffPixelWidth);
    }
    public record ViewportSize(int Width, int Height);
}
