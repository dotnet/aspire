// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;
/// <summary>
///
/// </summary>
public enum ProbeType
{
    /// <summary>
    ///
    /// </summary>
    Startup = 0,

    /// <summary>
    ///
    /// </summary>
    Readiness = 1,

    /// <summary>
    ///
    /// </summary>
    Liveness = 2,
}
