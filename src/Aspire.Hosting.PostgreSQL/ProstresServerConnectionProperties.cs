// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies the available properties for configuring a Prostres server connection.
/// </summary>
/// <remarks>Use this enumeration to identify individual connection properties, such as host, port, username, and
/// password, when configuring or querying Prostres server connection settings.</remarks>
public enum ProstresServerConnectionProperties
{
    /// <summary>
    /// Host
    /// </summary>
    Host,

    /// <summary>
    /// Port
    /// </summary>
    Port,

    /// <summary>
    /// Username
    /// </summary>
    Username,

    /// <summary>
    /// Password
    /// </summary>
    Password,
}
