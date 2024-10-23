// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Configuration;

/// <summary>
/// The valid authentication modes for the dashboard frontend
/// </summary>
public enum FrontendAuthMode
{
    /// <summary>
    /// Unsecured should only be used during local development
    /// </summary>
    Unsecured,

    /// <summary>
    /// OpenIdConnect authentication
    /// </summary>
    OpenIdConnect,

    /// <summary>
    /// BrowserToken authentication
    /// </summary>
    BrowserToken
}
