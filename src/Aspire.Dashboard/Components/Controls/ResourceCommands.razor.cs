// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components;

public partial class ResourceCommands : ComponentBase
{
    [Parameter]
    public required IList<CommandViewModel> Commands { get; set; }

    [Parameter]
    public EventCallback<CommandViewModel> CommandSelected { get; set; }
}
