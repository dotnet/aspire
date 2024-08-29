// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class MenuButtonItem
{
    public bool IsDivider { get; set; }
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public Func<Task>? OnClick { get; set; }
}
