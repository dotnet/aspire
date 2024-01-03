// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ResourceErrorInfoButton
{
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    private void NavigateToResourceErrorStructuredLogs(ResourceViewModel resource, bool isError)
    {
        if (isError)
        {
            NavigationManager.NavigateTo($"/StructuredLogs/{resource.Uid}?level=error");
        }
        else
        {
            NavigationManager.NavigateTo($"/StructuredLogs/{resource.Uid}");
        }
    }
}
