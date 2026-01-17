// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agent.Tools;

/// <summary>
/// Tool for creating new Aspire projects.
/// </summary>
internal interface IAspireNewTool
{
    Task<string> ExecuteAsync(string template, string name, string? outputDir);
}

/// <summary>
/// Tool for adding integrations to an AppHost.
/// </summary>
internal interface IAspireAddTool
{
    Task<string> ExecuteAsync(string integration, string appHostPath);
}

/// <summary>
/// Tool for running the Aspire AppHost.
/// </summary>
internal interface IAspireRunTool
{
    Task<string> ExecuteAsync(string appHostPath, bool watch);
}

/// <summary>
/// Tool for running Aspire diagnostics.
/// </summary>
internal interface IAspireDoctorTool
{
    Task<string> ExecuteAsync();
}

/// <summary>
/// Tool for listing available integrations.
/// </summary>
internal interface IListIntegrationsTool
{
    Task<string> ExecuteAsync(string? filter);
}

/// <summary>
/// Tool for getting integration documentation.
/// </summary>
internal interface IGetIntegrationDocsTool
{
    Task<string> ExecuteAsync(string packageId);
}
