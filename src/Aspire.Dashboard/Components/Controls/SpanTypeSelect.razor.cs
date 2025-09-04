// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class SpanTypeSelect : ComponentBase
{
    [Parameter, EditorRequired]
    public required List<SelectViewModel<SpanType>> SpanTypes { get; set; }

    [Parameter, EditorRequired]
    public required SelectViewModel<SpanType> SpanType { get; set; }

    [Parameter]
    public EventCallback<SelectViewModel<SpanType>> SpanTypeChanged { get; set; }

    [Parameter, EditorRequired]
    public required Func<Task> HandleSelectedSpanTypeChangedAsync { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    public required ViewportInformation ViewportInformation { get; set; }

    private async Task HandleSelectedSpanTypeChangedInternalAsync()
    {
        await SpanTypeChanged.InvokeAsync(SpanType);
        await HandleSelectedSpanTypeChangedAsync();
    }
}

