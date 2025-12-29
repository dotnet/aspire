// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;

namespace Aspire.Hosting.CodeGeneration.TypeScript.Tests;

public class TypeScriptCodeGeneratorTests
{
    private readonly TypeScriptCodeGenerator _generator = new();

    [Fact]
    public void Language_ReturnsTypeScript()
    {
        Assert.Equal("TypeScript", _generator.Language);
    }

    [Fact]
    public async Task EmbeddedResource_RemoteAppHostClientTs_MatchesSnapshot()
    {
        var assembly = typeof(TypeScriptCodeGenerator).Assembly;
        var resourceName = "Aspire.Hosting.CodeGeneration.TypeScript.Resources.RemoteAppHostClient.ts";

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        await Verify(content, extension: "ts")
            .UseFileName("RemoteAppHostClient");
    }

    [Fact]
    public async Task EmbeddedResource_TypesTs_MatchesSnapshot()
    {
        var assembly = typeof(TypeScriptCodeGenerator).Assembly;
        var resourceName = "Aspire.Hosting.CodeGeneration.TypeScript.Resources.types.ts";

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        await Verify(content, extension: "ts")
            .UseFileName("types");
    }

    [Fact]
    public async Task EmbeddedResource_PackageJson_MatchesSnapshot()
    {
        var packageJson = TypeScriptCodeGenerator.GetPackageJsonTemplate();

        await Verify(packageJson, extension: "json")
            .UseFileName("package");
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        // Act
        var files = _generator.GenerateDistributedApplication(model);

        // Assert
        await Verify(files)
            .UseFileName("GeneratedDistributedApplication");
    }

    [Fact]
    public void RoType_Methods_ReturnsExpectedMethodsForConfigurationManager()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();
        var configType = model.BuilderModel.ProxyTypes.Keys
            .FirstOrDefault(t => t.Name == "ConfigurationManager");

        Assert.NotNull(configType);

        // Act
        var methodNames = configType.Methods.Select(m => m.Name).Order().ToArray();

        // Assert
        Assert.Equal(["Dispose", "GetChildren", "GetSection"], methodNames);
    }

    [Fact]
    public void BuilderModel_ProxyTypes_DiscoverNestedPropertyTypes()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        // Act
        var proxyTypeNames = model.BuilderModel.ProxyTypes.Values
            .Select(p => p.ProxyClassName)
            .ToHashSet();

        // Assert - verify top-level framework types
        Assert.Contains("ConfigurationManagerProxy", proxyTypeNames);
        Assert.Contains("HostEnvironmentProxy", proxyTypeNames);
        Assert.Contains("ServiceProviderProxy", proxyTypeNames);

        // Assert - verify nested types discovered recursively
        Assert.Contains("FileProviderProxy", proxyTypeNames);           // From IHostEnvironment.ContentRootFileProvider
        Assert.Contains("TempFileSystemServiceProxy", proxyTypeNames);  // Discovered recursively
    }

    [Fact]
    public void RoType_Methods_ReturnsExpectedMethodsForServiceProvider()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();
        var serviceProviderType = model.BuilderModel.ProxyTypes.Keys
            .FirstOrDefault(t => t.Name == "IServiceProvider");

        Assert.NotNull(serviceProviderType);

        // Act
        var methodNames = serviceProviderType.Methods.Select(m => m.Name).Order().ToArray();

        // Assert
        Assert.Equal(["GetService"], methodNames);
    }

    private static Aspire.Hosting.CodeGeneration.Models.ApplicationModel CreateApplicationModelFromTestAssembly()
    {
        // Get the path to this test assembly
        var testAssemblyPath = typeof(TestRedisResource).Assembly.Location;
        var testAssemblyDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Also need the Aspire.Hosting assembly for runtime types
        var hostingAssemblyPath = typeof(Aspire.Hosting.DistributedApplication).Assembly.Location;
        var hostingAssemblyDir = Path.GetDirectoryName(hostingAssemblyPath)!;

        // Need Microsoft.Extensions.Hosting for IHost type used by DistributedApplication
        var extensionsHostingDir = Path.GetDirectoryName(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location)!;

        // Get the runtime assemblies directory for core types
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var assemblyPaths = new[] { testAssemblyDir, hostingAssemblyDir, extensionsHostingDir, runtimeDir };

        // Load the test assembly using AssemblyLoaderContext
        var context = new AssemblyLoaderContext();

        // First load the Aspire.Hosting assembly to get WellKnownTypes
        var hostingAssembly = context.LoadAssembly("Aspire.Hosting", assemblyPaths);
        if (hostingAssembly is null)
        {
            throw new InvalidOperationException("Failed to load Aspire.Hosting assembly");
        }

        var wellKnownTypes = new WellKnownTypes(context);

        // Now load the test assembly
        var testAssembly = context.LoadAssembly("Aspire.Hosting.CodeGeneration.TypeScript.Tests", assemblyPaths);
        if (testAssembly is null)
        {
            throw new InvalidOperationException("Failed to load test assembly");
        }

        // Create an IntegrationModel from the test assembly
        var integrationModel = IntegrationModel.Create(wellKnownTypes, testAssembly);

        // Create an ApplicationModel
        return Aspire.Hosting.CodeGeneration.Models.ApplicationModel.Create([integrationModel], "/test/app", context);
    }
}
