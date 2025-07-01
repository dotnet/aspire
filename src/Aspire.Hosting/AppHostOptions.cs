// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Configuration options for the Aspire application host.
/// </summary>
/// <remarks>
/// Should be used only for AppHost services configuration purposes,
/// there is a more convenient API to use in the flow at <see cref="DistributedApplicationExecutionContext"/>
/// </remarks>
internal class AppHostOptions
{
    /// <summary>
    /// Configuration section name for the Aspire application host.
    /// </summary>
    public const string Section = "AppHost";

    /// <summary>
    /// The operation to be performed by the application host.
    /// </summary>
    public string? Operation { get; set; }

    public bool IsExecMode() => string.Equals(Operation, "exec", StringComparison.OrdinalIgnoreCase);
}
