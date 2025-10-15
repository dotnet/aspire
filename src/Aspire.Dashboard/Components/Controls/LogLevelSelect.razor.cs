// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class LogLevelSelect : ComponentBase
{
    [Parameter, EditorRequired]
    public required List<SelectViewModel<LogLevel?>> LogLevels { get; set; }

    [Parameter, EditorRequired]
    public required SelectViewModel<LogLevel?> LogLevel { get; set; }

    [Parameter]
    public EventCallback<SelectViewModel<LogLevel?>> LogLevelChanged { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> HandleSelectedLogLevelChangedAsync { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    private async Task HandleSelectedLogLevelChangedInternalAsync()
    {
        await LogLevelChanged.InvokeAsync(LogLevel);
        await HandleSelectedLogLevelChangedAsync();
    }
}

