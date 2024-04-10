// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Aspire.Dashboard.Components.Pages;

public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
