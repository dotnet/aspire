// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Enum representing the type of probe.
/// </summary>
public enum ProbeType
{
    /// <summary>
    /// Startup probe.
    /// </summary>
    Startup = 0,

    /// <summary>
    /// Readiness probe.
    /// </summary>
    Readiness = 1,

    /// <summary>
    /// Liveness probe.
    /// </summary>
    Liveness = 2,
}
