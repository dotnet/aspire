// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Resize;

public class BrowserDimensionWatcher : ComponentBase
{
    [Parameter]
    public ViewportInformation? ViewportInformation { get; set; }

    [Parameter]
    public EventCallback<ViewportInformation?> ViewportInformationChanged { get; set; }

    [Inject]
    public required IJSRuntime JS { get; init; }

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
        if (ViewportInformation?.Width == viewportSize.Width && ViewportInformation?.Height == viewportSize.Height)
        {
            return;
        }

        ViewportInformation = GetViewportInformation(viewportSize);
        await ViewportInformationChanged.InvokeAsync(ViewportInformation);
    }

    private static ViewportInformation GetViewportInformation(ViewportSize viewportSize)
    {
        // set our mobile cutoff at 768 pixels, which is ~medium tablet size
        var isDesktop = viewportSize.Width > 768;
        return new ViewportInformation(!isDesktop, viewportSize.Height, viewportSize.Width);
    }

    public record ViewportSize(int Width, int Height);
}

