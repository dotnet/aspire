// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class SqlServerContainerImageTags
{
    public const string Registry = "mcr.microsoft.com";
    public const string Image = "mssql/server";
    // Tracking tag: 2022-latest. NOTE: latest  digest fails the tests. We need to investigate.
    public const string Digest = "sha256:c4369c38385eba011c10906dc8892425831275bb035d5ce69656da8e29de50d8";
}
