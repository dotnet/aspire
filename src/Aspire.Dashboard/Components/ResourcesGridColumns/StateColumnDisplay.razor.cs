// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.ResourcesGridColumns;

public partial class StateColumnDisplay
{
    [Parameter, EditorRequired]
    public required Dictionary<OtlpApplication, int>? UnviewedErrorCounts { get; set; }

    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }
}
