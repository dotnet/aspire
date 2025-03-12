// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal sealed class Integration(string packageName, string packageVersion, string packageShortName)
{
    public string PackageShortName { get; } = packageShortName;
    public string PackageName { get; } = packageName;
    public string PackageVersion { get; } = packageVersion;
}

internal interface IIntegrationLookup
{
    IEnumerable<Integration> GetIntegrations();
}

internal sealed class IntegrationLookup : IIntegrationLookup
{
    public IEnumerable<Integration> GetIntegrations()
    {
        // HACK: Just to get the rest working.

        return [
            new Integration(
                "Aspire.Hosting.Redis",
                "9.1",
                "redis"
            ),
            new Integration(
                "Aspire.Hosting.PostgreSql",
                "9.1",
                "postgres"
            ),
        ];
    }
}