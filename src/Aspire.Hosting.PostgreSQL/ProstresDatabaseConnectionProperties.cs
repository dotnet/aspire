// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Specifies the available properties for configuring a Prostres database connection.
/// </summary>
/// <remarks>Use this enumeration to identify individual connection parameters, such as host, port, username,
/// password, and database name, when working with Prostres database connection settings.</remarks>
public enum ProstresDatabaseConnectionProperties
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

    /// <summary>
    /// Database
    /// </summary>
    Database
}
