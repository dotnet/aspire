// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools for discovering available telemetry fields and their values.
/// </summary>
internal sealed class AspireFieldsMcpTools
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly IDashboardClient _dashboardClient;
    private readonly ILogger<AspireFieldsMcpTools> _logger;

    public AspireFieldsMcpTools(
        TelemetryRepository telemetryRepository,
        IDashboardClient dashboardClient,
        ILogger<AspireFieldsMcpTools> logger)
    {
        _telemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
        _logger = logger;
    }
}
