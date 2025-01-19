// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.MariaDB;

internal static class MariaDBContainerImageTags
{
    /// <remarks>docker.io</remarks>
    public const string Registry = "docker.io";

    /// <remarks>library/mariadb</remarks>
    public const string Image = "library/mariadb";

    /// <remarks>11.7</remarks>
    public const string Tag = "11.6";

    /// <remarks>library/phpmyadmin</remarks>
    public const string PhpMyAdminImage = "library/phpmyadmin";

    /// <remarks>5.2</remarks>
    public const string PhpMyAdminTag = "5.2";
}
