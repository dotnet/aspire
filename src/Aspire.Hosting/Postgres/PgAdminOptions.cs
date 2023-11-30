// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Postgres;

/// <summary>
/// PgAdmin container image options.
/// </summary>
public sealed class PgAdminOptions
{
    /// <summary>
    /// Gets or sets the default email to access the application.
    /// </summary>
    public string DefaultEmail { get; set; } = "user@domain.com";
    /// <summary>
    /// Gets or sets the default password to access the application.
    /// </summary>
    public string DefaultPassword { get; set; } = "SuperSecret";
}
