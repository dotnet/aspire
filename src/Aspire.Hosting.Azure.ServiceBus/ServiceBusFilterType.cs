// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus;

/// <summary>
/// Rule filter types.
/// </summary>
public enum ServiceBusFilterType
{
    /// <summary>
    /// SqlFilter.
    /// </summary>
    SqlFilter,

    /// <summary>
    /// CorrelationFilter.
    /// </summary>
    CorrelationFilter,
}
