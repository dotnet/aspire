// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
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
            var properties = result.RootElement.GetProperty("Properties");
            var sdkVersion = properties.GetProperty("AspireHostingSDKVersion").GetString();
            Assert.Equal("9.5.0-test", sdkVersion);

            // Should have fallback flag
            Assert.True(result.RootElement.GetProperty("Fallback").GetBoolean());
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
            var items = result.RootElement.GetProperty("Items");
            var packageRefs = items.GetProperty("PackageReference").EnumerateArray().ToArray();
            
            Assert.Equal(2, packageRefs.Length);
            
            var appHostPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "Aspire.Hosting.AppHost");
            Assert.NotEqual(default(JsonElement), appHostPkg);
            Assert.Equal("9.5.0-test", appHostPkg.GetProperty("Version").GetString());
            
            var redisPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "Aspire.Hosting.Redis");
            Assert.NotEqual(default(JsonElement), redisPkg);
            Assert.Equal("9.4.1", redisPkg.GetProperty("Version").GetString());
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
            var items = result.RootElement.GetProperty("Items");
            var projectRefs = items.GetProperty("ProjectReference").EnumerateArray().ToArray();
            
            Assert.Equal(2, projectRefs.Length);
            
            var serviceDefaultsRef = projectRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString()!.Contains("ServiceDefaults"));
            Assert.NotEqual(default(JsonElement), serviceDefaultsRef);
            
            var webAppRef = projectRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString()!.Contains("WebApp"));
            Assert.NotEqual(default(JsonElement), webAppRef);
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