// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Rule filter types.
/// </summary>
public enum AzureServiceBusFilterType
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
