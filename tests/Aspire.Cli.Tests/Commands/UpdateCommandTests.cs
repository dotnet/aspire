// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace Aspire.Cli.Tests.Commands;

public class UpdateCommandTests(ITestOutputHelper outputHelper)
{
    private sealed class RecordingPackagingService : IPackagingService
    {
        public bool GetChannelsCalled { get; private set; }
        public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
        {
            GetChannelsCalled = true;
            // Return a default channel for testing
            var channel = PackageChannel.CreateImplicitChannel(null!);
            return Task.FromResult<IEnumerable<PackageChannel>>([channel]);
        }
    }

    private sealed class TestProjectLocator : IProjectLocator
    {
        private readonly FileInfo _projectFile;
        public TestProjectLocator(FileInfo projectFile) => _projectFile = projectFile;
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default)
            => Task.FromResult<FileInfo?>(_projectFile);
        public Task CreateSettingsFileIfNotExistsAsync(FileInfo projectFile, CancellationToken cancellationToken = default)
        {
            // Use parameters to satisfy analyzers.
            _ = projectFile;
            _ = cancellationToken;
            // Reference instance field so method can't be static (CA1822).
            _ = _projectFile;
            return Task.CompletedTask;
        }

        public Task<List<FileInfo>> FindAppHostProjectFilesAsync(string searchDirectory, CancellationToken cancellationToken)
        {
            return Task.FromResult<List<FileInfo>>([_projectFile]);
        }
    }

    [Fact]
    public async Task UpdateCommand_DoesNotFailEarlyForCentralPackageManagement()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create CPM marker file with an Aspire package.
        await File.WriteAllTextAsync(Path.Combine(workspace.WorkspaceRoot.FullName, "Directory.Packages.props"), 
            """
            <Project>
                <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                </PropertyGroup>
                <ItemGroup>
                    <PackageVersion Include="Aspire.Hosting.AppHost" Version="9.4.1" />
                </ItemGroup>
            </Project>
            """);

        // Create minimal app host project.
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, 
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.4.1" />
                <ItemGroup>
                    <PackageReference Include="Aspire.Hosting.AppHost" />
                </ItemGroup>
            </Project>
            """);

        var recordingPackagingService = new RecordingPackagingService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator(appHostProject);
            options.PackagingServiceFactory = _ => recordingPackagingService;
            // Interaction service so selection prompt would succeed when reached.
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();
            // Add mock CLI runner with SearchPackages support
            options.DotNetCliRunnerFactory = _ => new TestServices.TestDotNetCliRunner()
            {
                GetProjectItemsAndPropertiesAsyncCallback = (projectFile, items, properties, options, cancellationToken) =>
                {
                    var itemsAndProperties = new System.Text.Json.Nodes.JsonObject();
                    itemsAndProperties.WithSdkVersion("9.4.1");
                    itemsAndProperties.WithPackageReferenceWithoutVersion("Aspire.Hosting.AppHost");

                    var json = itemsAndProperties.ToJsonString();
                    var document = System.Text.Json.JsonDocument.Parse(json);
                    return (0, document);
                },
                SearchPackagesAsyncCallback = (_, query, _, _, _, _, _, _) =>
                {
                    var packages = new List<Aspire.Shared.NuGetPackageCli>();

                    packages.Add(query switch
                    {
                        "Aspire.AppHost.Sdk" => new Aspire.Shared.NuGetPackageCli { Id = "Aspire.AppHost.Sdk", Version = "9.5.0", Source = "nuget.org" },
                        "Aspire.Hosting.AppHost" => new Aspire.Shared.NuGetPackageCli { Id = "Aspire.Hosting.AppHost", Version = "9.5.0", Source = "nuget.org" },
                        _ => throw new InvalidOperationException($"Unexpected package query: {query}"),
                    });

                    return (0, packages.ToArray());
                }
            };
        });

        var provider = services.BuildServiceProvider();
        var root = provider.GetRequiredService<RootCommand>();
        var result = root.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.True(recordingPackagingService.GetChannelsCalled); // Ensure we reach channel prompting.
    }
}

internal static class MSBuildJsonDocumentExtensionsForUpdate
{
    public static JsonObject WithSdkVersion(this JsonObject root, string sdkVersion)
    {
        JsonObject properties = new JsonObject();
        if (!root.TryAdd("Properties", properties))
        {
            properties = root["Properties"]!.AsObject();
        }

        properties.Add("AspireHostingSDKVersion", JsonValue.Create<string>(sdkVersion));
        return root;
    }

    public static JsonObject WithPackageReferenceWithoutVersion(this JsonObject root, string packageId)
    {
        JsonObject items = new JsonObject();
        items.Add("ProjectReference", new JsonArray());
        items.Add("PackageReference", new JsonArray());

        if (!root.TryAdd("Items", items))
        {
            items = root["Items"]!.AsObject();
        }

        JsonArray packageReferences = new JsonArray();
        if (!items.TryAdd("PackageReference", packageReferences))
        {
            packageReferences = items["PackageReference"]!.AsArray();
        }

        JsonObject newPackageReference = new JsonObject
        {
            { "Identity", JsonValue.Create<string>(packageId) }
            // No Version property for CPM
        };
        packageReferences.Add(newPackageReference);

        return root;
    }
}
