// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Components.Controls;

public partial class FluentIconSwitch
{
    private async Task OnToggleInternalAsync()
    {
        Value = Value is not true;
        await ValueChanged.InvokeAsync(Value.Value);
        await OnToggle.InvokeAsync();
    }
}
