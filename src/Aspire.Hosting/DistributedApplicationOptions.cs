// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Options for configuring the behavior of <see cref="DistributedApplication.CreateBuilder(DistributedApplicationOptions)"/>.
/// </summary>
public sealed class DistributedApplicationOptions
{
    /// <summary>
    /// The command line arguments.
    /// </summary>
    public string[]? Args { get; set; }

    /// <summary>
    /// The AssemblyName of the AppHost project for loading configuration attributes; if not set defaults to Assembly.GetEntryAssembly().
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Determines whether the dashboard is disabled.
    /// </summary>
    public bool DisableDashboard { get; set; }

    /// <summary>
    /// Logs each of the service's logs to ILogger on shutdown.
    /// </summary>
    public bool WriteLogsToLoggerOnShutdown { get; set; }

    internal bool DashboardEnabled => !DisableDashboard;
}
