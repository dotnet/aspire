// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli;

internal sealed class Integration(string packageName, string packageVersion, string packageShortName, Func<string?, string> appHostSnippet)
{
    public string PackageShortName { get; } = packageShortName;
    public string PackageName { get; } = packageName;
    public string PackageVersion { get; } = packageVersion;
    public Func<string?, string> AppHostSnippet { get; } = appHostSnippet;

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
                "redis",
                (resourceName) => $"builder.AddRedis(\"{resourceName ?? "redis"}\");"
            ),
            new Integration(
                "Aspire.Hosting.PostgreSql",
                "9.1",
                "postgres",
                (resourceName) => $"builder.AddPostgres(\"{resourceName ?? "postgres"}\");"
            ),
        ];
    }
}