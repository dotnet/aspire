// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Assistant.Ghcp;

public class GhcpInfoResponse
{
    public string State { get; set; } = GhcpState.Unknown;
    public string? Launcher { get; set; }
    public List<GhcpModelResponse>? Models { get; set; }
}
