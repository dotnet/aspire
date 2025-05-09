// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

// TODO: This is a temporary fix for https://github.com/dotnet/aspire/issues/8343 and https://github.com/microsoft/fluentui-blazor/issues/3600
// Remove once FluentUI is fixed and dashboard is using the fixed version.
[CascadingTypeParameter(nameof(TOption))]
public partial class FixedFluentCombobox<TOption> : FluentCombobox<TOption> where TOption : notnull
{
    protected override async Task ChangeHandlerAsync(ChangeEventArgs e)
    {
        await InvokeAsync(async () => await base.ChangeHandlerAsync(e));
    }
}
