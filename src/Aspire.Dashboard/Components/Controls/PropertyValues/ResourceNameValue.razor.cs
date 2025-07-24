// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Components.Controls.PropertyValues;

public partial class ResourceNameValue
{
    [Parameter, EditorRequired]
    public required string Value { get; set; }

    [Parameter, EditorRequired]
    public required string HighlightText { get; set; }

    [Parameter, EditorRequired]
    public required OtlpApplication Resource { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    private Icon? _resourceIcon;

    protected override void OnParametersSet()
    {
        var resource = DashboardClient.GetResource(Resource.ApplicationKey.ToString());
        _resourceIcon = resource != null
            ? ResourceIconHelpers.GetIconForResource(resource, IconSize.Size16, IconVariant.Regular)
            : new Icons.Regular.Size16.AppFolder();
    }

    private Task OnClickAsync()
    {
        NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl(Resource.ApplicationKey.ToString()));
        return Task.CompletedTask;
    }
}
