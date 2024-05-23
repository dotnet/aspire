using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Resize;

public partial class BrowserDimensionWatcher : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    public event BrowserResizedEventHandler? OnBrowserResize;

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "/Components/Resize/BrowserDimensionWatcher.razor.js");
            var viewport = await _jsModule.InvokeAsync<ViewportSize>("getWindowDimensions");
            OnBrowserResize?.Invoke(this, new BrowserResizeEventArgs(GetViewportInformation(viewport)));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            await JSInteropHelpers.SafeDisposeAsync(_jsModule);
        }
    }

    private static ViewportInformation GetViewportInformation(ViewportSize viewportSize)
    {
        var isSmall = viewportSize.Width < 768;
        return new ViewportInformation(!isSmall, viewportSize.Height, viewportSize.Width);
    }

    public delegate void BrowserResizedEventHandler(object sender, BrowserResizeEventArgs e);

    private record ViewportSize(int Width, int Height);
}

