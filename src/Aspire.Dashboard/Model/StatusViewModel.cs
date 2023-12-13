// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class StatusViewModel
{
    public required string? State { get; set; }
    public int? ExitCode { get; set; }
}
