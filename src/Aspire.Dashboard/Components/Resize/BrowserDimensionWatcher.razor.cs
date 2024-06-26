// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Resize;

public class BrowserDimensionWatcher : ComponentBase
{
    [Parameter] public ViewportInformation? ViewportInformation { get; set; }

    [Parameter] public EventCallback<ViewportInformation?> ViewportInformationChanged { get; set; }

    [Inject] public required IJSRuntime JS { get; init; }

    [Inject] public required DimensionManager DimensionManager { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var viewport = await JS.InvokeAsync<ViewportSize>("window.getWindowDimensions");
            ViewportInformation = GetViewportInformation(viewport);
            await ViewportInformationChanged.InvokeAsync(ViewportInformation);

            await JS.InvokeVoidAsync("window.listenToWindowResize", DotNetObjectReference.Create(this));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public async Task OnResizeAsync(ViewportSize viewportSize)
    {
        var newViewportInformation = GetViewportInformation(viewportSize);

        if (newViewportInformation.IsDesktop != ViewportInformation!.IsDesktop || newViewportInformation.IsUltraLowHeight != ViewportInformation.IsUltraLowHeight)
        {
            ViewportInformation = newViewportInformation;
            await ViewportInformationChanged.InvokeAsync(newViewportInformation);
            DimensionManager.InvokeOnBrowserDimensionsChanged();
        }
    }

    private static ViewportInformation GetViewportInformation(ViewportSize viewportSize)
    {
        return new ViewportInformation(IsDesktop: viewportSize.Width > 768, IsUltraLowHeight: viewportSize.Height < 400);
    }

    public static ViewportInformation Create(int height, int width)
    {
        return new ViewportInformation(IsDesktop: width > 768, IsUltraLowHeight: height < 400);
    }

    public record ViewportSize(int Width, int Height);
}
