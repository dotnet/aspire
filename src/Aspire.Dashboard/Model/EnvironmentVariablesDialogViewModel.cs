// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class EnvironmentVariablesDialogViewModel
{
    public required List<EnvironmentVariableViewModel> EnvironmentVariables { get; init; }
    public bool ShowSpecOnlyToggle { get; set; }
}
