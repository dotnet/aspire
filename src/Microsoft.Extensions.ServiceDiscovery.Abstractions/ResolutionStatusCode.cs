// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Status codes for <see cref="ResolutionStatus"/>.
/// </summary>
public enum ResolutionStatusCode
{
    /// <summary>
    /// Resolution has not been performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Resolution is pending completion.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Resolution did not find any end points for the specified service.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Resolution was successful.
    /// </summary>
    Success = 3,

    /// <summary>
    /// Resolution was canceled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Resolution failed.
    /// </summary>
    Error = 5,
}
