// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.Projects;

public class ProjectUpdaterTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task UpdateProjectFileAsync_DoesAttemptToUpdateIfNoUpdatesRequired()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var srcFolder = workspace.CreateDirectory("src");

        var serviceDefaultsFolder = workspace.CreateDirectory("UpdateTester.ServiceDefaults");
        var serviceDefaultsProjectFile = new FileInfo(Path.Combine(serviceDefaultsFolder.FullName, "UpdateTester.ServiceDefaults.csproj"));
        await File.WriteAllTextAsync(
            serviceDefaultsProjectFile.FullName,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <IsAspireSharedProject>true</IsAspireSharedProject>
                </PropertyGroup>
                <ItemGroup>
                    <FrameworkReference Include="Microsoft.AspNetCore.App" />
                    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.7.0" />
                    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.4.1" />
                    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
                    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
                    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
                    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
                    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
                </ItemGroup>
            </Project>
            """);

        var webAppFolder = workspace.CreateDirectory("UpdateTester.WebApp");
        var serviceDefaultsRelativePath = Path.GetRelativePath(webAppFolder.FullName, serviceDefaultsProjectFile.FullName);
        var webAppProjectFile = new FileInfo(Path.Combine(webAppFolder.FullName, "UpdateTester.WebApp.csproj"));
        await File.WriteAllTextAsync(
            webAppProjectFile.FullName,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="{{serviceDefaultsRelativePath}}" />
                </ItemGroup>
                <ItemGroup>
                    <PackageReference Include="Aspire.StackExchange.Redis.OutputCaching" Version="9.4.1" />
                </ItemGroup>
            </Project>
            """);

        var appHostFolder = workspace.CreateDirectory("UpdateTester.AppHost");
        var webAppRelativePath = Path.GetRelativePath(appHostFolder.FullName, webAppProjectFile.FullName);
        var appHostProjectFile = new FileInfo(Path.Combine(appHostFolder.FullName, "UpdateTester.AppHost.csproj"));
        await File.WriteAllTextAsync(
            appHostProjectFile.FullName,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.4.1" />
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <UserSecretsId>51921b16-6f3e-4e07-b4df-0bc7aab5902e</UserSecretsId>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="{{webAppRelativePath}}" />
                </ItemGroup>

                <ItemGroup>
                    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.4.1" />
                    <PackageReference Include="Aspire.Hosting.Redis" Version="9.4.1" />
                </ItemGroup>
            </Project>
            """);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, config =>
        {
            config.DotNetCliRunnerFactory = (sp) =>
            {
                return new TestDotNetCliRunner()
                {
                    SearchPackagesAsyncCallback = (_, query, _, _, _, _, _, _) =>
                    {
                        var packages = new List<NuGetPackageCli>();

                        packages.Add(query switch
                        {
                            "Aspire.AppHost.Sdk" => new NuGetPackageCli { Id = "Aspire.AppHost.Sdk", Version = "9.4.1", Source = "nuget.org" },
                            "Aspire.Hosting.AppHost" => new NuGetPackageCli { Id = "Aspire.Hosting.AppHost", Version = "9.4.1", Source = "nuget.org" },
                            "Aspire.Hosting.Redis" => new NuGetPackageCli { Id = "Aspire.Hosting.Redis", Version = "9.4.1", Source = "nuget.org" },
                            "Aspire.StackExchange.Redis.OutputCaching" => new NuGetPackageCli { Id = "Aspire.StackExchange.Redis.OutputCaching", Version = "9.4.1", Source = "nuget.org" },
                            "Microsoft.Extensions.ServiceDiscovery" => new NuGetPackageCli { Id = "Microsoft.Extensions.ServiceDiscovery", Version = "9.4.1", Source = "nuget.org" },
                            _ => throw new InvalidOperationException("Unexpected package query."),
                        });

                        return (0, packages.ToArray());
                    },

                    GetProjectItemsAndPropertiesAsyncCallback = (projectFile, _, _, _, _) =>
                    {
                        var itemsAndProperties = new JsonObject();

                        if (projectFile.FullName == appHostProjectFile.FullName)
                        {
                            itemsAndProperties.WithSdkVersion("9.4.1");
                            itemsAndProperties.WithPackageReference("Aspire.Hosting.AppHost", "9.4.1");
                            itemsAndProperties.WithPackageReference("Aspire.Hosting.Redis", "9.4.1");
                            itemsAndProperties.WithProjectReference(webAppProjectFile.FullName);
                        }
                        else if (projectFile.FullName == webAppProjectFile.FullName)
                        {
                            itemsAndProperties.WithPackageReference("Aspire.StackExchange.Redis.OutputCaching", "9.4.1");
                            itemsAndProperties.WithProjectReference(serviceDefaultsProjectFile.FullName);
                        }
                        else if (projectFile.FullName == serviceDefaultsProjectFile.FullName)
                        {
                            itemsAndProperties.WithPackageReference("Microsoft.ServiceDiscovery.Extensions", "9.4.1");
                        }
                        else
                        {
                            throw new InvalidOperationException("Unexpected project file.");
                        }

                        var json = itemsAndProperties.ToJsonString();
                        var document = JsonDocument.Parse(json);
                        return (0, document);
                    }
                };
            };

            config.InteractionServiceFactory = (sp) =>
            {
                var interactionService = new TestConsoleInteractionService();
                interactionService.ConfirmCallback = (promptText, defaultValue) =>
                {
                    throw new InvalidOperationException("Should not prompt when no work required.");
                };

                return interactionService;
            };
        });
        var provider = services.BuildServiceProvider();

        // Services we need for project updater.
        var logger = provider.GetRequiredService<ILogger<ProjectUpdater>>();
        var runner = provider.GetRequiredService<IDotNetCliRunner>();
        var interactionService = provider.GetRequiredService<IInteractionService>();
        var cache = provider.GetRequiredService<IMemoryCache>();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var channels = await packagingService.GetChannelsAsync();
        var implicitChannel = channels.Single(c => c.Type == PackageChannelType.Implicit);

        // If this throws then it means that the updater prompted
        // for confirmation to do an update when no update was required!
        var projectUpdater = new ProjectUpdater(logger, runner, interactionService, cache, executionContext);
        var updateResult = await projectUpdater.UpdateProjectAsync(appHostProjectFile, implicitChannel).WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.False(updateResult.UpdatedApplied);
    }

    private static Aspire.Cli.CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory)
    {
        // NOTE: This would normally be in the users home directory, but for tests we create
        //       it in the temporary workspace directory.
        var settingsDirectory = workingDirectory.CreateSubdirectory(".aspire");
        var hivesDirectory = settingsDirectory.CreateSubdirectory("hives");
        return new CliExecutionContext(workingDirectory, hivesDirectory);
    }
}

internal static class MSBuildJsonDocumentExtensions
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

    public static JsonObject WithPackageReference(this JsonObject root, string packageId, string packageVersion)
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
            { "Identity", JsonValue.Create<string>(packageId) },
            { "Version", JsonValue.Create<string>(packageVersion) }
        };
        packageReferences.Add(newPackageReference);

        return root;
    }

    public static JsonObject WithProjectReference(this JsonObject root, string fullPath)
    {
        JsonObject items = new JsonObject();
        if (!root.TryAdd("Items", items))
        {
            items = root["Items"]!.AsObject();
        }

        JsonArray projectReferences = new JsonArray();
        if (!items.TryAdd("ProjectReference", projectReferences))
        {
            projectReferences = items["ProjectReference"]!.AsArray();
        }

        JsonObject newProjectReference = new JsonObject
        {
            { "FullPath", JsonValue.Create<string>(fullPath) }
        };
        projectReferences.Add(newProjectReference);

        return root;
    }
}