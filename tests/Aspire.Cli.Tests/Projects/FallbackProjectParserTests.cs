// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class FallbackProjectParserTests
{
    [Fact]
    public void ParseProject_ExtractsAspireAppHostSdk()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var projectFile = Path.Combine(tempDir, $"Test{Guid.NewGuid()}.csproj");
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
            </Project>
            """;

        try
        {
            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            if (!result.TryGetPropertyValue("Properties", out var propertiesNode) || propertiesNode is not JsonObject properties)
            {
                Assert.Fail("Properties section not found");
                return;
            }
            
            if (!properties.TryGetPropertyValue("AspireHostingSDKVersion", out var sdkVersionNode))
            {
                Assert.Fail("AspireHostingSDKVersion property not found");
                return;
            }
            
            var sdkVersion = sdkVersionNode?.GetValue<string>();
            Assert.Equal("9.5.0-test", sdkVersion);

            // Should have fallback flag
            if (!result.TryGetPropertyValue("Fallback", out var fallbackNode))
            {
                Assert.Fail("Fallback property not found");
                return;
            }
            Assert.True(fallbackNode?.GetValue<bool>());
        }
        finally
        {
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
        }
    }

    [Fact]
    public void ParseProject_ExtractsPackageReferences()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var projectFile = Path.Combine(tempDir, $"Test{Guid.NewGuid()}.csproj");
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                <ItemGroup>
                    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.0-test" />
                    <PackageReference Include="Aspire.Hosting.Redis" Version="9.4.1" />
                </ItemGroup>
            </Project>
            """;

        try
        {
            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            if (!result.TryGetPropertyValue("Items", out var itemsNode) || itemsNode is not JsonObject items)
            {
                Assert.Fail("Items section not found");
                return;
            }
            
            if (!items.TryGetPropertyValue("PackageReference", out var packageRefsNode) || packageRefsNode is not JsonArray packageRefs)
            {
                Assert.Fail("PackageReference array not found");
                return;
            }
            
            Assert.Equal(2, packageRefs.Count);
            
            var appHostPkg = packageRefs.FirstOrDefault(p => 
                p is JsonObject pkg && 
                pkg.TryGetPropertyValue("Identity", out var identityNode) &&
                identityNode?.GetValue<string>() == "Aspire.Hosting.AppHost");
            Assert.NotNull(appHostPkg);
            
            if (appHostPkg is JsonObject appHostPkgObj && 
                appHostPkgObj.TryGetPropertyValue("Version", out var versionNode))
            {
                Assert.Equal("9.5.0-test", versionNode?.GetValue<string>());
            }
            else
            {
                Assert.Fail("Version not found for Aspire.Hosting.AppHost");
            }
            
            var redisPkg = packageRefs.FirstOrDefault(p => 
                p is JsonObject pkg && 
                pkg.TryGetPropertyValue("Identity", out var identityNode) &&
                identityNode?.GetValue<string>() == "Aspire.Hosting.Redis");
            Assert.NotNull(redisPkg);
            
            if (redisPkg is JsonObject redisPkgObj && 
                redisPkgObj.TryGetPropertyValue("Version", out var redisVersionNode))
            {
                Assert.Equal("9.4.1", redisVersionNode?.GetValue<string>());
            }
            else
            {
                Assert.Fail("Version not found for Aspire.Hosting.Redis");
            }
        }
        finally
        {
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
        }
    }

    [Fact]
    public void ParseProject_ExtractsProjectReferences()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var projectFile = Path.Combine(tempDir, $"Test{Guid.NewGuid()}.csproj");
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                <ItemGroup>
                    <ProjectReference Include="../ServiceDefaults/ServiceDefaults.csproj" />
                    <ProjectReference Include="../WebApp/WebApp.csproj" />
                </ItemGroup>
            </Project>
            """;

        try
        {
            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            if (!result.TryGetPropertyValue("Items", out var itemsNode) || itemsNode is not JsonObject items)
            {
                Assert.Fail("Items section not found");
                return;
            }
            
            if (!items.TryGetPropertyValue("ProjectReference", out var projectRefsNode) || projectRefsNode is not JsonArray projectRefs)
            {
                Assert.Fail("ProjectReference array not found");
                return;
            }
            
            Assert.Equal(2, projectRefs.Count);
            
            var serviceDefaultsRef = projectRefs.FirstOrDefault(p => 
                p is JsonObject proj && 
                proj.TryGetPropertyValue("Identity", out var identityNode) &&
                identityNode?.GetValue<string>()?.Contains("ServiceDefaults") == true);
            Assert.NotNull(serviceDefaultsRef);
            
            var webAppRef = projectRefs.FirstOrDefault(p => 
                p is JsonObject proj && 
                proj.TryGetPropertyValue("Identity", out var identityNode) &&
                identityNode?.GetValue<string>()?.Contains("WebApp") == true);
            Assert.NotNull(webAppRef);
        }
        finally
        {
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
        }
    }

    [Fact]
    public void ParseProject_InvalidXml_ThrowsProjectUpdaterException()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var projectFile = Path.Combine(tempDir, $"Test{Guid.NewGuid()}.csproj");
        var invalidProjectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                <!-- Missing closing tag -->
                <ItemGroup>
                    <PackageReference Include="Test" Version="1.0.0" />
            """;

        try
        {
            File.WriteAllText(projectFile, invalidProjectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act & Assert
            Assert.Throws<ProjectUpdaterException>(() => 
                parser.ParseProject(new FileInfo(projectFile)));
        }
        finally
        {
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
        }
    }
}