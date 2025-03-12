// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Cli;

internal sealed class Integration(string packageName, string packageVersion, string packageShortName)
{
    public string PackageShortName { get; } = packageShortName;
    public string PackageName { get; } = packageName;
    public string PackageVersion { get; } = packageVersion;
}

internal interface IIntegrationLookup
{
    Task<IEnumerable<Integration>> GetIntegrationsAsync(CancellationToken cancellationToken);
}

internal sealed class IntegrationLookup : IIntegrationLookup
{
/*
var nugetCache = Path.Combine(Path.GetTempPath(), FolderPrefix, "packages.json");

        if (!File.Exists(nugetCache) || new FileInfo(nugetCache).CreationTimeUtc < DateTime.UtcNow.AddDays(-1))
        {
            using var httpClient = new HttpClient();
            var result = httpClient.GetStringAsync("https://azuresearch-usnc.nuget.org/query?q=tag:aspire+integration+hosting&take=1000").Result;
            File.WriteAllText(nugetCache, result);
        }

        var doc = JsonObject.Parse(File.ReadAllText(nugetCache));
        _packageNames = doc!["data"]!.AsArray().Select(x => $"{x!.AsObject()["id"]}@{x.AsObject()["version"]}").ToHashSet();
*/

    public async Task<IEnumerable<Integration>> GetIntegrationsAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var result = await httpClient.GetStringAsync(
            "https://azuresearch-usnc.nuget.org/query?q=tag:aspire+integration+hosting&take=1000",
            cancellationToken).ConfigureAwait(false);

        var doc = JsonDocument.Parse(result);

        var integrations = new List<Integration>();

        var data = doc.RootElement.GetProperty("data");
        
        for (int index = 0; index < data.GetArrayLength(); index++)
        {
            var item = data[index];
            var id = item.GetProperty("id").GetString();
            var version = item.GetProperty("version").GetString();

            integrations.Add(new Integration(
                id!,
                version!,
                id!
            ));
        }

        return integrations;
    }
}