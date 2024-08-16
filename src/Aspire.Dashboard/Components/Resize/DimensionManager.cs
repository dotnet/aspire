// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Resize;

public class DimensionManager
{
    public event BrowserDimensionsChangedEventHandler? OnBrowserDimensionsChanged;

    public bool IsResizing { get; set; }

    internal void InvokeOnBrowserDimensionsChanged(ViewportInformation newViewportInformation)
    {
        OnBrowserDimensionsChanged?.Invoke(this, new BrowserDimensionsChangedEventArgs(newViewportInformation));
    }
}

public delegate void BrowserDimensionsChangedEventHandler(object sender, BrowserDimensionsChangedEventArgs e);

public class BrowserDimensionsChangedEventArgs(ViewportInformation viewportInformation) : EventArgs
{
    public ViewportInformation ViewportInformation { get; } = viewportInformation;
}
