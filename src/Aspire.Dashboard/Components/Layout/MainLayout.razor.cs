// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.Fast.Components.FluentUI.DesignTokens;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private BaseLayerLuminance BaseLayerLuminance { get; set; } = default!;

    [Inject]    
    private PersistentComponentState ApplicationState { get; set; } = default!;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private StandardLuminance _baseLayerLuminance = StandardLuminance.LightMode;
    private PersistingComponentStateSubscription _persistingSubscription;

    private ElementReference _container = default!;

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
                "dark" => StandardLuminance.DarkMode,
                _ => StandardLuminance.LightMode
            };
        }
    }

    protected override void OnInitialized()
    {
        // See if we got a base layer luminance value from the cookie and set the value
        // This will avoid a flash of white if the last system theme and current system theme are both dark
        if (ApplicationState.TryTakeFromJson<StandardLuminance>("baseLayerStandardLuminance", out var restoredBaseLayerStandardLuminance))
        {
            _baseLayerLuminance = restoredBaseLayerStandardLuminance;
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
            _baseLayerLuminance = isSystemThemeDark ? StandardLuminance.DarkMode : StandardLuminance.LightMode;

            await BaseLayerLuminance.SetValueFor(_container, _baseLayerLuminance.GetLuminanceValue());
            StateHasChanged();
        }
    }

    private Task PersistBaseLayerLuminance()
    {
        // Persist the base layer luminance value from pre-rendering (when we pull it out of the
        // cookie) to rendering (when setting it is important.
        ApplicationState.PersistAsJson("baseLayerStandardLuminance", _baseLayerLuminance);
        return Task.CompletedTask;
    }
}

// Uncomment the code below to overload the GetLuminanceValue extension method and use custom luminosity values.
// Probably needs to be move to it's own file too.
/*
public static class StandardLuminanceExtensions
{
    private const float LightMode = 1.0f;
    private const float DarkMode = 0.15f;

    public static float GetLuminanceValue(this StandardLuminance value)
    {
        return value == StandardLuminance.LightMode ? LightMode : DarkMode;
    }
}
*/
