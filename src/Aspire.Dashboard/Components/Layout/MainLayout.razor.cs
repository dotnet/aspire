// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private PersistentComponentState ApplicationState { get; set; } = default!;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private float _baseLayerLuminance = StandardLuminance.LightMode.GetLuminanceValue();
    private PersistingComponentStateSubscription _persistingSubscription;

    protected override void OnParametersSet()
    {
        if (HttpContext is not null)
        {
            _persistingSubscription = ApplicationState.RegisterOnPersisting(PersistBaseLayerLuminance);

            // Look to see if we have a cookie saying what the last system theme was
            // and set the base layer luminance based on that
            var lastSystemTheme = HttpContext.Request.Cookies["lastSystemTheme"];
            _baseLayerLuminance = lastSystemTheme switch
            {
                "dark" => StandardLuminance.DarkMode.GetLuminanceValue(),
                _ => StandardLuminance.LightMode.GetLuminanceValue()
            };
        }
    }

    protected override void OnInitialized()
    {
        // See if we got a base layer luminance value from the cookie and set the value
        // This will avoid a flash of white if the last system theme and current system theme are both dark
        if (ApplicationState.TryTakeFromJson<float>("baseLayerLuminance", out var restoredBaseLayerLuminance))
        {
            _baseLayerLuminance = restoredBaseLayerLuminance;
            StateHasChanged();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Use javascript to determine the current system theme and set the theme cookie
            // based on that value. Then use the result of that to update the base layer luminance.
            // If the system theme hasn't changed from last time, this will have no effect.
            // If it has, we might have a flash of (last system theme color) before the current
            // system theme color takes effect.
            var isSystemThemeDark = await JS.InvokeAsync<bool>("setThemeCookie");
            _baseLayerLuminance = isSystemThemeDark ? StandardLuminance.DarkMode.GetLuminanceValue() : StandardLuminance.LightMode.GetLuminanceValue();

            StateHasChanged();
        }
    }

    private Task PersistBaseLayerLuminance()
    {
        // Persist the base layer luminance value from pre-rendering (when we pull it out of the
        // cookie) to rendering (when setting it is important.
        ApplicationState.PersistAsJson("baseLayerLuminance", _baseLayerLuminance);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _persistingSubscription.Dispose();
    }
}
