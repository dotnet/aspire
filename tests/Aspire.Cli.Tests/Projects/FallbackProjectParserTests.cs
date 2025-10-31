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
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.csproj");
            var projectContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                </Project>
                """;

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
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_ExtractsPackageReferences()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.csproj");
            var projectContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                    <ItemGroup>
                        <PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.0-test" />
                        <PackageReference Include="Aspire.Hosting.Redis" Version="9.4.1" />
                    </ItemGroup>
                </Project>
                """;

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
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_ExtractsProjectReferences()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.csproj");
            var projectContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                    <ItemGroup>
                        <ProjectReference Include="../ServiceDefaults/ServiceDefaults.csproj" />
                        <ProjectReference Include="../WebApp/WebApp.csproj" />
                    </ItemGroup>
                </Project>
                """;

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
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_InvalidXml_ThrowsProjectUpdaterException()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.csproj");
            var invalidProjectContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0-test" />
                    <!-- Missing closing tag -->
                    <ItemGroup>
                        <PackageReference Include="Test" Version="1.0.0" />
                """;

            File.WriteAllText(projectFile, invalidProjectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act & Assert
            Assert.Throws<ProjectUpdaterException>(() => 
                parser.ParseProject(new FileInfo(projectFile)));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_ExtractsAspireAppHostSdk()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                #:sdk Aspire.AppHost.Sdk@13.0.0-preview.1.25519.5
                #:package Aspire.Hosting.NodeJs@9.5.1

                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            var properties = result.RootElement.GetProperty("Properties");
            var sdkVersion = properties.GetProperty("AspireHostingSDKVersion").GetString();
            Assert.Equal("13.0.0-preview.1.25519.5", sdkVersion);

            // Should have fallback flag
            Assert.True(result.RootElement.GetProperty("Fallback").GetBoolean());
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_ExtractsPackageReferences()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                #:sdk Aspire.AppHost.Sdk@13.0.0-preview.1.25519.5
                #:package Aspire.Hosting.NodeJs@9.5.1
                #:package Aspire.Hosting.Python@9.5.1
                #:package Aspire.Hosting.Redis@9.5.1
                #:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.8.0

                #pragma warning disable ASPIREHOSTINGPYTHON001

                var builder = DistributedApplication.CreateBuilder(args);
                var cache = builder.AddRedis("cache");
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            var items = result.RootElement.GetProperty("Items");
            var packageRefs = items.GetProperty("PackageReference").EnumerateArray().ToArray();
            
            Assert.Equal(4, packageRefs.Length);
            
            var nodeJsPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "Aspire.Hosting.NodeJs");
            Assert.NotEqual(default(JsonElement), nodeJsPkg);
            Assert.Equal("9.5.1", nodeJsPkg.GetProperty("Version").GetString());
            
            var pythonPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "Aspire.Hosting.Python");
            Assert.NotEqual(default(JsonElement), pythonPkg);
            Assert.Equal("9.5.1", pythonPkg.GetProperty("Version").GetString());

            var redisPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "Aspire.Hosting.Redis");
            Assert.NotEqual(default(JsonElement), redisPkg);
            Assert.Equal("9.5.1", redisPkg.GetProperty("Version").GetString());

            var toolkitPkg = packageRefs.FirstOrDefault(p => 
                p.GetProperty("Identity").GetString() == "CommunityToolkit.Aspire.Hosting.NodeJS.Extensions");
            Assert.NotEqual(default(JsonElement), toolkitPkg);
            Assert.Equal("9.8.0", toolkitPkg.GetProperty("Version").GetString());
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_NoPackageReferences()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                #:sdk Aspire.AppHost.Sdk@9.5.0

                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            var items = result.RootElement.GetProperty("Items");
            var packageRefs = items.GetProperty("PackageReference").EnumerateArray().ToArray();
            
            Assert.Empty(packageRefs);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_WithWildcardVersion()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                #:sdk Aspire.AppHost.Sdk@*
                #:package Aspire.Hosting.Redis@*

                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            var properties = result.RootElement.GetProperty("Properties");
            var sdkVersion = properties.GetProperty("AspireHostingSDKVersion").GetString();
            Assert.Equal("*", sdkVersion);

            var items = result.RootElement.GetProperty("Items");
            var packageRefs = items.GetProperty("PackageReference").EnumerateArray().ToArray();
            Assert.Single(packageRefs);
            Assert.Equal("*", packageRefs[0].GetProperty("Version").GetString());
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_NoProjectReferences()
    {
        // Arrange - single-file apphosts don't support project references
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                #:sdk Aspire.AppHost.Sdk@9.5.0

                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert
            var items = result.RootElement.GetProperty("Items");
            var projectRefs = items.GetProperty("ProjectReference").EnumerateArray().ToArray();
            
            Assert.Empty(projectRefs);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_SingleFileAppHost_NoSdkDirective()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.cs");
            var projectContent = """
                // Missing SDK directive
                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """;

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act
            var result = parser.ParseProject(new FileInfo(projectFile));

            // Assert - should return null SDK version
            var properties = result.RootElement.GetProperty("Properties");
            var sdkVersion = properties.GetProperty("AspireHostingSDKVersion");
            Assert.Equal(JsonValueKind.Null, sdkVersion.ValueKind);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ParseProject_UnsupportedFileType_ThrowsProjectUpdaterException()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var projectFile = Path.Combine(tempDir.FullName, $"Test{Guid.NewGuid()}.txt");
            var projectContent = "Some random content";

            File.WriteAllText(projectFile, projectContent);
            var parser = new FallbackProjectParser(NullLogger<FallbackProjectParser>.Instance);

            // Act & Assert
            var exception = Assert.Throws<ProjectUpdaterException>(() => 
                parser.ParseProject(new FileInfo(projectFile)));
            Assert.Contains("Unsupported project file type", exception.Message);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}