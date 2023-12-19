// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class UnreadLogErrorsBadge
{
    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private void ViewResourceErrorStructuredLogsUrl(ResourceViewModel resource)
    {
        NavigationManager.NavigateTo($"/StructuredLogs/{resource.Uid}?level=error");
    }
}
