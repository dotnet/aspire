// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;

namespace Aspire.Dashboard.Model;

public class LogViewerViewModel
{
    public LogEntries LogEntries { get; }= new();
    public string? ResourceName { get; set; }

}
