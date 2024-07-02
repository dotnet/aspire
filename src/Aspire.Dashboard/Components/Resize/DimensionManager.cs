// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Resize;

public class DimensionManager
{
    public event EventHandler? OnBrowserDimensionsChanged;

    internal void InvokeOnBrowserDimensionsChanged()
    {
        OnBrowserDimensionsChanged?.Invoke(this, EventArgs.Empty);
    }
}
