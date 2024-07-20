// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class LogLevelSelect : ComponentBase
{
    private async Task HandleSelectedLogLevelChangedInternalAsync()
    {
        await LogLevelChanged.InvokeAsync(LogLevel);
        await HandleSelectedLogLevelChangedAsync();
    }
}

