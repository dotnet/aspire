// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Postgres;
public sealed class PgAdminOptions
{
    public string DefaultEmail { get; set; } = "user@domain.com";
    public string DefaultPassword { get; set; } = "SuperSecret";
}
