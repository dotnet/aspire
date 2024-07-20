// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.ConsoleLogs;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Model;

public class LogViewerViewModel(IOptions<DashboardOptions> options)
{
    public LogEntries LogEntries { get; } = new(options.Value.Frontend.MaxConsoleLogCount);
    public string? ResourceName { get; set; }
}
