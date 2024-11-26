// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Postgres;

internal static class PostgresContainerImageTags
{
    /// <summary>docker.io</summary>
    public const string Registry = "docker.io";

    /// <summary>library/postgres</summary>
    public const string Image = "library/postgres";

    /// <summary>17.0</summary>
    public const string Tag = "17.0";

    /// <summary>docker.io</summary>
    public const string PgAdminRegistry = "docker.io";

    /// <summary>dpage/pgadmin4</summary>
    public const string PgAdminImage = "dpage/pgadmin4";

    /// <summary>8.12</summary>
    public const string PgAdminTag = "8.12";

    /// <summary>docker.io</summary>
    public const string PgWebRegistry = "docker.io";

    /// <summary>sosedoff/pgweb</summary>
    public const string PgWebImage = "sosedoff/pgweb";

    /// <summary>0.16.1</summary>
    public const string PgWebTag = "0.16.1";
}
