// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Components.Controls;

public partial class ExplainErrorsButton
{
    [Inject]
    public required IAIContextProvider AIContextProvider { get; init; }

    [Inject]
    public required IStringLocalizer<AIAssistant> Loc { get; init; }

    /// <summary>
    /// This parameter is required because this control could be added and removed from the page.
    /// When the control is re-added to the page it gets the value back via the parameter.
    /// </summary>
    [Parameter]
    public bool HasErrors { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    /// Called when data grid data is refreshed. Forces the control to re-render.
    /// </summary>
    public void UpdateHasErrors(bool hasErrors)
    {
        HasErrors = hasErrors;
        StateHasChanged();
    }
}
