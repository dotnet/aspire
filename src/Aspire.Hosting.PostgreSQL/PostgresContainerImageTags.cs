// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Postgres;

internal static class PostgresContainerImageTags
{
    /// <remarks>docker.io</remarks>
    public const string Registry = "docker.io";

    /// <remarks>library/postgres</remarks>
    public const string Image = "library/postgres";

    /// <remarks>17.4</remarks>
    public const string Tag = "17.4";

    /// <remarks>docker.io</remarks>
    public const string PgAdminRegistry = "docker.io";

    /// <remarks>dpage/pgadmin4</remarks>
    public const string PgAdminImage = "dpage/pgadmin4";

    /// <remarks>9.2.0</remarks>
    public const string PgAdminTag = "9.2.0";

    /// <remarks>docker.io</remarks>
    public const string PgWebRegistry = "docker.io";

    /// <remarks>sosedoff/pgweb</remarks>
    public const string PgWebImage = "sosedoff/pgweb";

    /// <remarks>0.16.2</remarks>
    public const string PgWebTag = "0.16.2";
}
