// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

public class MenuButtonItem
{
    public bool IsDivider { get; set; }
    public string? Title { get; set; }
    public Icon? Icon { get; set; }
    public Func<Task>? OnClick { get; set; }
}
