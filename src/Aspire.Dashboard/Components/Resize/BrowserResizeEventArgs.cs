// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Resize;

public class BrowserResizeEventArgs(ViewportInformation viewportInformation) : EventArgs
{
    public ViewportInformation ViewportInformation { get; } = viewportInformation;
}
