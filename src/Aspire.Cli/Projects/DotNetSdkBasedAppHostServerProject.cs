// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Packaging;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// AppHost server project that uses NuGet packages via the Aspire.AppHost.Sdk.
/// This is the standard mode for users who have the .NET SDK installed.
/// </summary>
internal sealed class DotNetSdkBasedAppHostServerProject : DotNetBasedAppHostServerProject
{
    public DotNetSdkBasedAppHostServerProject(
        string appPath,
        string socketPath,
        IDotNetCliRunner dotNetCliRunner,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILogger<DotNetSdkBasedAppHostServerProject> logger,
        string? projectModelPath = null)
        : base(appPath, socketPath, dotNetCliRunner, packagingService, configurationService, logger, projectModelPath)
    {
    }

    protected override XDocument CreateProjectFile(string sdkVersion, IEnumerable<(string Name, string Version)> packages)
    {
        var template = $"""
            <Project Sdk="Aspire.AppHost.Sdk/{sdkVersion}">
                <PropertyGroup>
                    <OutputType>exe</OutputType>
                    <TargetFramework>{TargetFramework}</TargetFramework>
                    <AssemblyName>{AssemblyName}</AssemblyName>
                    <OutDir>{BuildFolder}</OutDir>
                    <UserSecretsId>{_userSecretsId}</UserSecretsId>
                    <IsAspireHost>true</IsAspireHost>
                </PropertyGroup>
                <!-- Disable Aspire SDK code generation -->
                <Target Name="_CSharpWriteHostProjectMetadataSources" />
                <Target Name="_CSharpWriteProjectMetadataSources" />
            </Project>
            """;

        var doc = XDocument.Parse(template);

        // Add package references - SDK provides Aspire.Hosting.AppHost (which brings Aspire.Hosting)
        // We need to add: RemoteHost, code gen package, and any integration packages
        var explicitPackages = packages
            .Where(p => !p.Name.Equals("Aspire.Hosting", StringComparison.OrdinalIgnoreCase) &&
                        !p.Name.Equals("Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Always add RemoteHost - required for the RPC server
        explicitPackages.Add(("Aspire.Hosting.RemoteHost", sdkVersion));

        var packageRefs = explicitPackages.Select(p => new XElement("PackageReference",
            new XAttribute("Include", p.Name),
            new XAttribute("Version", p.Version)));
        doc.Root!.Add(new XElement("ItemGroup", packageRefs));

        return doc;
    }
}
