// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class GlobalOptions
{
    public LogLevel LogLevel { get; init; }
    public bool NoHotReload { get; init; }
    public bool NonInteractive { get; init; }

    /// <summary>
    /// Path to binlog file (absolute or relative to working directory, includes .binlog extension),
    /// or null to not generate binlog files.
    /// </summary>
    public string? BinaryLogPath { get; init; }
}
