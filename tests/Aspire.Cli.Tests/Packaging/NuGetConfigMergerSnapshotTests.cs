// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Cli.Packaging;
using Aspire.Cli.NuGet;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Packaging;

// Initial focused snapshot tests for NuGetConfigMerger. These allow us to feed a literal NuGet.config
// then exercise CreateOrUpdateAsync and verify the resulting XML in a stable, readable snapshot.
public class NuGetConfigMergerSnapshotTests
{
    private readonly ITestOutputHelper _output;

    public NuGetConfigMergerSnapshotTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed class FakeNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
    }

    private static async Task<FileInfo> WriteConfigAsync(DirectoryInfo dir, string content)
    {
        var path = Path.Combine(dir.FullName, "NuGet.config");
        await File.WriteAllTextAsync(path, content);
        return new FileInfo(path);
    }

        [Theory]
        [InlineData("stable")]
        [InlineData("daily")]
        [InlineData("pr-1234")]
        public async Task Merge_WithRealPackagingServiceChannel_ProducesExpectedXml(string channelName)
        {
            using var workspace = TemporaryWorkspace.Create(_output);
            var root = workspace.WorkspaceRoot;

            // Empty hives directory ensures deterministic channel set (no PR channels)
            var hivesDir = root.CreateSubdirectory("hives");
            // Add a deterministic PR hive for testing realistic PR channel mappings.
            hivesDir.CreateSubdirectory("pr-1234");
            var executionContext = new CliExecutionContext(root, hivesDir);
            var packagingService = new PackagingService(executionContext, new FakeNuGetPackageCache());

            // Existing config purposely minimal (no packageSourceMapping yet)
            await WriteConfigAsync(root,
                """
                <?xml version="1.0"?>
                <configuration>
                    <packageSources>
                        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                    </packageSources>
                </configuration>
                """);

            var channels = await packagingService.GetChannelsAsync();
            // Filter to explicit channels here so we never select the implicit ("default") channel
            // which has no mappings and would produce a no-op merge (nothing meaningful to snapshot).
            var channel = channels.First(c => c.Type is PackageChannelType.Explicit && string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));

            await NuGetConfigMerger.CreateOrUpdateAsync(root, channel);

            var updated = XDocument.Load(Path.Combine(root.FullName, "NuGet.config"));
            var xmlString = updated.ToString();

            // Normalize machine-specific absolute hive paths in PR channel snapshots for stability
            if (channelName.StartsWith("pr-", StringComparison.OrdinalIgnoreCase))
            {
                var hivePath = Path.Combine(hivesDir.FullName, channelName);
                xmlString = xmlString.Replace(hivePath, "{PR_HIVE}");
            }

            await Verify(xmlString, extension: "xml")
                .UseFileName($"NuGetConfigMerger.{channelName}");
        }
}
