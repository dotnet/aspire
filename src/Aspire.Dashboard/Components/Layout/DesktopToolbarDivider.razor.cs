// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class DesktopToolbarDivider
{
    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }
}
