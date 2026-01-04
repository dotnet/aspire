// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;

namespace Aspire.Hosting.CodeGeneration.TypeScript.Tests;

public class AtsTypeScriptCodeGeneratorTests
{
    private readonly AtsTypeScriptCodeGenerator _generator = new();

    [Fact]
    public void Language_ReturnsTypeScript()
    {
        Assert.Equal("TypeScript", _generator.Language);
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        // Act
        var files = _generator.GenerateDistributedApplication(model);

        // Assert
        Assert.Contains("aspire.ts", files.Keys);
        Assert.Contains("RemoteAppHostClient.ts", files.Keys);
        Assert.Contains("types.ts", files.Keys);

        await Verify(files["aspire.ts"], extension: "ts")
            .UseFileName("AtsGeneratedAspire");
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_IncludesCapabilities()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        // Assert that capabilities are discovered
        var capabilities = model.IntegrationModels.Values
            .SelectMany(im => im.Capabilities)
            .ToList();

        Assert.NotEmpty(capabilities);

        // Check for specific capabilities
        Assert.Contains(capabilities, c => c.CapabilityId == "aspire.test/addTestRedis@1");
        Assert.Contains(capabilities, c => c.CapabilityId == "aspire.test/withPersistence@1");
        Assert.Contains(capabilities, c => c.CapabilityId == "aspire.test/withOptionalString@1");
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        var capabilities = model.IntegrationModels.Values
            .SelectMany(im => im.Capabilities)
            .ToList();

        // Assert method names are derived correctly
        var addTestRedis = capabilities.First(c => c.CapabilityId == "aspire.test/addTestRedis@1");
        Assert.Equal("addTestRedis", addTestRedis.MethodName);

        var withPersistence = capabilities.First(c => c.CapabilityId == "aspire.test/withPersistence@1");
        Assert.Equal("withPersistence", withPersistence.MethodName);
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_CapturesParameters()
    {
        // Arrange
        using var model = CreateApplicationModelFromTestAssembly();

        var capabilities = model.IntegrationModels.Values
            .SelectMany(im => im.Capabilities)
            .ToList();

        // Assert parameters are captured
        var addTestRedis = capabilities.First(c => c.CapabilityId == "aspire.test/addTestRedis@1");
        Assert.Equal(2, addTestRedis.Parameters.Count);
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "name" && p.AtsTypeId == "string");
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "port" && p.IsOptional);
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
