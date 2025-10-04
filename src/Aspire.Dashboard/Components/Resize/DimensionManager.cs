// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Resize;

public class DimensionManager
{
    private ViewportInformation? _viewportInformation;
    private ViewportSize? _viewportSize;
    private bool? _isAISidebarOpen;

    public event ViewportSizeChangedEventHandler? OnViewportSizeChanged;
    public event ViewportInformationChangedEventHandler? OnViewportInformationChanged;

    public ViewportInformation ViewportInformation => _viewportInformation ?? throw new ArgumentNullException(nameof(_viewportInformation));
    public ViewportSize ViewportSize => _viewportSize ?? throw new ArgumentNullException(nameof(_viewportSize));
    public bool IsAISidebarOpen => _isAISidebarOpen ?? throw new ArgumentNullException(nameof(_isAISidebarOpen));
    public bool HasViewportSize => _viewportSize != null;

    internal void InvokeOnViewportSizeChanged(ViewportSize newViewportSize, bool isAISidebarOpen)
    {
        _viewportSize = newViewportSize;
        _isAISidebarOpen = isAISidebarOpen;
        OnViewportSizeChanged?.Invoke(this, new ViewportSizeChangedEventArgs(newViewportSize, isAISidebarOpen));
    }

    internal void InvokeOnViewportInformationChanged(ViewportInformation newViewportInformation)
    {
        _viewportInformation = newViewportInformation;
        OnViewportInformationChanged?.Invoke(this, new ViewportInformationChangedEventArgs(newViewportInformation));
    }
}

public delegate void ViewportInformationChangedEventHandler(object sender, ViewportInformationChangedEventArgs e);
public delegate void ViewportSizeChangedEventHandler(object sender, ViewportSizeChangedEventArgs e);

public class ViewportInformationChangedEventArgs(ViewportInformation viewportInformation) : EventArgs
{
    public ViewportInformation ViewportInformation { get; } = viewportInformation;
}

public class ViewportSizeChangedEventArgs(ViewportSize viewportSize, bool isAISidebarOpen) : EventArgs
{
    public ViewportSize ViewportSize { get; } = viewportSize;
    public bool IsAISidebarOpen { get; } = isAISidebarOpen;
}
