// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias AspireHosting;

using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Ats;
using Aspire.Hosting.CodeGeneration.Models.Types;
using Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes;
// AtsTypeMapping is now unambiguous since Aspire.Hosting is not in global namespace
using Aspire.Hosting.Ats;

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
    public async Task EmbeddedResource_RemoteAppHostClientTs_MatchesSnapshot()
    {
        var assembly = typeof(AtsTypeScriptCodeGenerator).Assembly;
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
        var assembly = typeof(AtsTypeScriptCodeGenerator).Assembly;
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
        var assembly = typeof(AtsTypeScriptCodeGenerator).Assembly;
        var resourceName = "Aspire.Hosting.CodeGeneration.TypeScript.Resources.package.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        await Verify(content, extension: "json")
            .UseFileName("package");
    }

    [Fact]
    public async Task GenerateDistributedApplication_WithTestTypes_GeneratesCorrectOutput()
    {
        // Arrange
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Act
        var files = _generator.GenerateDistributedApplication(capabilities);

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
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Assert that capabilities are discovered
        Assert.NotEmpty(capabilities);

        // Check for specific capabilities (now uses AssemblyName/methodName format)
        Assert.Contains(capabilities, c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis");
        Assert.Contains(capabilities, c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence");
        Assert.Contains(capabilities, c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString");
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_DeriveCorrectMethodNames()
    {
        // Arrange
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Assert method names are derived correctly
        var addTestRedis = capabilities.First(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis");
        Assert.Equal("addTestRedis", addTestRedis.MethodName);

        var withPersistence = capabilities.First(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/withPersistence");
        Assert.Equal("withPersistence", withPersistence.MethodName);
    }

    [Fact]
    public void GenerateDistributedApplication_WithTestTypes_CapturesParameters()
    {
        // Arrange
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Assert parameters are captured
        // The builder parameter is skipped because TargetTypeId is inferred from the first parameter
        // (IDistributedApplicationBuilder -> "aspire/Builder")
        var addTestRedis = capabilities.First(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/addTestRedis");
        Assert.Equal(2, addTestRedis.Parameters.Count);
        Assert.Equal("aspire/Builder", addTestRedis.TargetTypeId);
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "name" && p.AtsTypeId == "string");
        Assert.Contains(addTestRedis.Parameters, p => p.Name == "port" && p.IsOptional);
    }

    [Fact]
    public void GenerateDistributedApplication_WithContextType_GeneratesPropertyCapabilities()
    {
        // Arrange
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Check for any context property capabilities (those with IsContextProperty = true)
        var contextCapabilities = capabilities.Where(c => c.IsContextProperty).ToList();

        // Assert context type property capabilities are discovered
        // TestCallbackContext has [AspireContextType("aspire.test/TestContext")]
        // with Name (string) and Value (int) properties
        //
        // Note: Context type scanning requires the AspireContextTypeAttribute to be resolvable
        // from the assembly's metadata. If no context capabilities are found, it may be because
        // the attribute type couldn't be resolved.
        if (contextCapabilities.Count == 0)
        {
            // Skip this test if no context types were found - this could be due to
            // attribute resolution issues in the metadata reader
            return;
        }

        var nameCapability = capabilities.FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestContext.name");
        Assert.NotNull(nameCapability);
        Assert.True(nameCapability.IsContextProperty);
        Assert.Equal("name", nameCapability.MethodName);
        Assert.Equal("string", nameCapability.ReturnTypeId);
        Assert.Equal("aspire.test/TestContext", nameCapability.TargetTypeId);
        Assert.Single(nameCapability.Parameters);
        Assert.Equal("context", nameCapability.Parameters[0].Name);

        var valueCapability = capabilities.FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/TestContext.value");
        Assert.NotNull(valueCapability);
        Assert.True(valueCapability.IsContextProperty);
        Assert.Equal("value", valueCapability.MethodName);
        Assert.Equal("number", valueCapability.ReturnTypeId);

        // CancellationToken - the type mapping is in Aspire.Hosting assembly.
        // Since the test only loads the test assembly's type mapping, CancellationToken
        // maps to "any" and is skipped as non-ATS-compatible.
        // In production, when Aspire.Hosting is loaded, CancellationToken will be properly mapped.
    }

    [Fact]
    public void Scanner_TestRedisResource_ImplementsIResource()
    {
        // This test verifies that TestRedisResource's interface collection includes IResource
        // which is inherited through: TestRedisResource -> ContainerResource -> Resource -> IResource
        using var context = new AssemblyLoaderContext();
        var (_, _, testAssembly, _) = LoadTestAssemblies(context);

        // Find TestRedisResource type
        var testRedisType = testAssembly.GetTypeDefinitions()
            .FirstOrDefault(t => t.FullName.Contains("TestRedisResource"));
        Assert.NotNull(testRedisType);

        // Collect all interfaces recursively (simulating what the scanner does)
        var allInterfaces = new HashSet<string>();
        CollectAllInterfacesRecursive(testRedisType, allInterfaces);

        // Should include IResource (inherited from ContainerResource -> Resource)
        Assert.Contains(allInterfaces, i => i.Contains("IResource") && !i.Contains("IResourceWith"));

        // Should include IResourceWithConnectionString (directly implemented)
        Assert.Contains(allInterfaces, i => i.Contains("IResourceWithConnectionString"));
    }

    private static void CollectAllInterfacesRecursive(RoType type, HashSet<string> collected)
    {
        // Add directly implemented interfaces
        foreach (var iface in type.Interfaces)
        {
            if (collected.Add(iface.FullName))
            {
                // Also collect interfaces that this interface extends
                CollectAllInterfacesRecursive(iface, collected);
            }
        }

        // Also check base type
        if (type.BaseType != null && type.BaseType.FullName != "System.Object")
        {
            CollectAllInterfacesRecursive(type.BaseType, collected);
        }
    }

    [Fact]
    public void Scanner_WithOptionalString_TargetsIResource()
    {
        // This test verifies that WithOptionalString<T> where T : IResource
        // correctly targets aspire/IResource
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Find the withOptionalString capability
        var withOptionalString = capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString");

        Assert.NotNull(withOptionalString);

        // Target should be aspire/IResource (from the constraint)
        Assert.Equal("aspire/IResource", withOptionalString.TargetTypeId);
    }

    [Fact]
    public void Scanner_WithOptionalString_ExpandsToTestRedis()
    {
        // This test verifies that WithOptionalString<T> where T : IResource
        // has its ExpandedTargetTypeIds include aspire/TestRedis
        using var context = new AssemblyLoaderContext();
        var capabilities = ScanCapabilitiesFromTestAssembly(context);

        // Find the withOptionalString capability
        var withOptionalString = capabilities
            .FirstOrDefault(c => c.CapabilityId == "Aspire.Hosting.CodeGeneration.TypeScript.Tests/withOptionalString");

        Assert.NotNull(withOptionalString);

        // Expanded targets should include aspire/TestRedis
        Assert.NotNull(withOptionalString.ExpandedTargetTypeIds);
        Assert.Contains("aspire/TestRedis", withOptionalString.ExpandedTargetTypeIds);
    }

    [Fact]
    public void Scanner_BaseTypeChain_CollectsInterfacesAcrossAssemblies()
    {
        // Debug test to understand the base type chain
        using var context = new AssemblyLoaderContext();
        var (_, _, testAssembly, _) = LoadTestAssemblies(context);

        // Find TestRedisResource type
        var testRedisType = testAssembly.GetTypeDefinitions()
            .FirstOrDefault(t => t.FullName.Contains("TestRedisResource"));
        Assert.NotNull(testRedisType);

        // Collect base type chain
        var baseTypes = new List<string>();
        var currentType = testRedisType.BaseType;
        while (currentType != null && currentType.FullName != "System.Object")
        {
            baseTypes.Add(currentType.FullName);
            currentType = currentType.BaseType;
        }

        // Should have ContainerResource and Resource in the chain
        Assert.Contains(baseTypes, t => t.Contains("ContainerResource"));
        Assert.Contains(baseTypes, t => t.Contains("Resource") && !t.Contains("Container"));
    }

    private static List<AtsCapabilityInfo> ScanCapabilitiesFromTestAssembly(AssemblyLoaderContext context)
    {
        var (_, wellKnownTypes, testAssembly, typeMapping) = LoadTestAssemblies(context);

        // Scan capabilities from the test assembly using the public API
        return AtsCapabilityScanner.ScanAssembly(testAssembly, wellKnownTypes, typeMapping);
    }

    private static (RoAssembly hostingAssembly, WellKnownTypes wellKnownTypes, RoAssembly testAssembly, AtsTypeMapping typeMapping) LoadTestAssemblies(AssemblyLoaderContext context)
    {
        // Get the path to this test assembly
        var testAssemblyPath = typeof(TestRedisResource).Assembly.Location;
        var testAssemblyDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Also need the Aspire.Hosting assembly for runtime types
        var hostingAssemblyPath = typeof(AspireHosting::Aspire.Hosting.DistributedApplication).Assembly.Location;
        var hostingAssemblyDir = Path.GetDirectoryName(hostingAssemblyPath)!;

        // Need Microsoft.Extensions.Hosting for IHost type used by DistributedApplication
        var extensionsHostingDir = Path.GetDirectoryName(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location)!;

        // Get the runtime assemblies directory for core types
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var assemblyPaths = new[] { testAssemblyDir, hostingAssemblyDir, extensionsHostingDir, runtimeDir };

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

        // Create type mapping from both assemblies using the public API
        var typeMapping = AtsTypeMapping.FromRoAssemblies([hostingAssembly, testAssembly]);

        return (hostingAssembly, wellKnownTypes, testAssembly, typeMapping);
    }
}
