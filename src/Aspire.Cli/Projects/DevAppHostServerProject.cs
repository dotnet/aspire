// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Xml.Linq;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Packaging;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// AppHost server project for local Aspire development.
/// Uses project references to the local Aspire repository (ASPIRE_REPO_ROOT).
/// </summary>
internal sealed class DevAppHostServerProject : DotNetBasedAppHostServerProject
{
    private readonly string _repoRoot;

    public DevAppHostServerProject(
        string appPath,
        string socketPath,
        string repoRoot,
        IDotNetCliRunner dotNetCliRunner,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILogger<DevAppHostServerProject> logger,
        string? projectModelPath = null)
        : base(appPath, socketPath, dotNetCliRunner, packagingService, configurationService, logger, projectModelPath)
    {
        _repoRoot = Path.GetFullPath(repoRoot) + Path.DirectorySeparatorChar;
    }

    protected override XDocument CreateProjectFile(string sdkVersion, IEnumerable<(string Name, string Version)> packages)
    {
        // Determine OS/architecture for DCP package name
        var (buildOs, buildArch) = GetBuildPlatform();
        var dcpPackageName = $"microsoft.developercontrolplane.{buildOs}-{buildArch}";
        var dcpVersion = GetDcpVersionFromRepo(_repoRoot, buildOs, buildArch);

        var template = $"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <OutputType>exe</OutputType>
                    <TargetFramework>{TargetFramework}</TargetFramework>
                    <AssemblyName>{AssemblyName}</AssemblyName>
                    <OutDir>{BuildFolder}</OutDir>
                    <UserSecretsId>{_userSecretsId}</UserSecretsId>
                    <IsAspireHost>true</IsAspireHost>
                    <IsPublishable>false</IsPublishable>
                    <SelfContained>false</SelfContained>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <WarningLevel>0</WarningLevel>
                    <EnableNETAnalyzers>false</EnableNETAnalyzers>
                    <EnableRoslynAnalyzers>false</EnableRoslynAnalyzers>
                    <RunAnalyzers>false</RunAnalyzers>
                    <NoWarn>$(NoWarn);1701;1702;1591;CS8019;CS1591;CS1573;CS0168;CS0219;CS8618;CS8625;CS1998;CS1999</NoWarn>
                    <!-- Properties for in-repo building -->
                    <RepoRoot>{_repoRoot}</RepoRoot>
                    <SkipValidateAspireHostProjectResources>true</SkipValidateAspireHostProjectResources>
                    <SkipAddAspireDefaultReferences>true</SkipAddAspireDefaultReferences>
                    <AspireHostingSDKVersion>42.42.42</AspireHostingSDKVersion>
                    <!-- DCP and Dashboard paths for local development -->
                    <DcpDir>$(NuGetPackageRoot){dcpPackageName}/{dcpVersion}/tools/</DcpDir>
                    <AspireDashboardDir>{_repoRoot}artifacts/bin/Aspire.Dashboard/Debug/net8.0/</AspireDashboardDir>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include="StreamJsonRpc" Version="2.22.23" />
                    <PackageReference Include="Google.Protobuf" Version="3.33.0" />
                </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(template);

        // Add project references for Aspire.Hosting.* packages, NuGet for others
        var projectRefGroup = new XElement("ItemGroup");
        var addedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var otherPackages = new List<(string Name, string Version)>();

        foreach (var (name, version) in packages)
        {
            if (name.StartsWith("Aspire.Hosting", StringComparison.OrdinalIgnoreCase))
            {
                var projectPath = Path.Combine(_repoRoot, "src", name, $"{name}.csproj");
                if (File.Exists(projectPath) && addedProjects.Add(name))
                {
                    projectRefGroup.Add(new XElement("ProjectReference",
                        new XAttribute("Include", projectPath),
                        new XElement("IsAspireProjectResource", "false")));
                }
            }
            else
            {
                otherPackages.Add((name, version));
            }
        }

        // Always add Aspire.Hosting project reference
        var hostingPath = Path.Combine(_repoRoot, "src", "Aspire.Hosting", "Aspire.Hosting.csproj");
        if (File.Exists(hostingPath) && addedProjects.Add("Aspire.Hosting"))
        {
            projectRefGroup.Add(new XElement("ProjectReference",
                new XAttribute("Include", hostingPath),
                new XElement("IsAspireProjectResource", "false")));
        }

        if (projectRefGroup.HasElements)
        {
            doc.Root!.Add(projectRefGroup);
        }

        if (otherPackages.Count > 0)
        {
            doc.Root!.Add(new XElement("ItemGroup",
                otherPackages.Select(p => new XElement("PackageReference",
                    new XAttribute("Include", p.Name),
                    new XAttribute("Version", p.Version)))));
        }

        // Add imports for in-repo AppHost building
        var appHostInTargets = Path.Combine(_repoRoot, "src", "Aspire.Hosting.AppHost", "build", "Aspire.Hosting.AppHost.in.targets");
        var sdkInTargets = Path.Combine(_repoRoot, "src", "Aspire.AppHost.Sdk", "SDK", "Sdk.in.targets");

        if (File.Exists(appHostInTargets))
        {
            doc.Root!.Add(new XElement("Import", new XAttribute("Project", appHostInTargets)));
        }
        if (File.Exists(sdkInTargets))
        {
            doc.Root!.Add(new XElement("Import", new XAttribute("Project", sdkInTargets)));
        }

        // Add Dashboard and RemoteHost project references
        var dashboardProject = Path.Combine(_repoRoot, "src", "Aspire.Dashboard", "Aspire.Dashboard.csproj");
        if (File.Exists(dashboardProject))
        {
            doc.Root!.Add(new XElement("ItemGroup",
                new XElement("ProjectReference", new XAttribute("Include", dashboardProject))));
        }

        var remoteHostProject = Path.Combine(_repoRoot, "src", "Aspire.Hosting.RemoteHost", "Aspire.Hosting.RemoteHost.csproj");
        if (File.Exists(remoteHostProject))
        {
            doc.Root!.Add(new XElement("ItemGroup",
                new XElement("ProjectReference", new XAttribute("Include", remoteHostProject))));
        }

        // Disable Aspire SDK code generation
        doc.Root!.Add(new XElement("Target", new XAttribute("Name", "_CSharpWriteHostProjectMetadataSources")));
        doc.Root!.Add(new XElement("Target", new XAttribute("Name", "_CSharpWriteProjectMetadataSources")));

        return doc;
    }

    private static (string Os, string Arch) GetBuildPlatform()
    {
        // OS mapping (matches MSBuild logic in Directory.Build.props)
        var os = OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "darwin"
            : "windows";

        // Architecture mapping
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => "386",
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => "amd64"
        };

        return (os, arch);
    }

    private static string GetDcpVersionFromRepo(string repoRoot, string buildOs, string buildArch)
    {
        const string fallbackVersion = "0.21.1";

        try
        {
            var versionsPropsPath = Path.Combine(repoRoot, "eng", "Versions.props");
            if (!File.Exists(versionsPropsPath))
            {
                return fallbackVersion;
            }

            var doc = XDocument.Load(versionsPropsPath);

            // Property name format: MicrosoftDeveloperControlPlane{os}{arch}Version
            // e.g., MicrosoftDeveloperControlPlanedarwinarm64Version
            var propertyName = $"MicrosoftDeveloperControlPlane{buildOs}{buildArch}Version";

            var version = doc.Descendants(propertyName).FirstOrDefault()?.Value;
            return version ?? fallbackVersion;
        }
        catch
        {
            return fallbackVersion;
        }
    }
}
