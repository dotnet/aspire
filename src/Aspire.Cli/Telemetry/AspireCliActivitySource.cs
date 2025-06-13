// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Telemetry;

/// <summary>
/// Provides a single ActivitySource for all Aspire CLI components.
/// </summary>
internal static class AspireCliActivitySource
{
    /// <summary>
    /// The shared ActivitySource instance for all CLI components.
    /// </summary>
    public static readonly ActivitySource Instance = new("Aspire.Cli");
}