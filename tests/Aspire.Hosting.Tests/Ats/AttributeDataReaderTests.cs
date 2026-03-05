// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using ThirdPartyExport = Aspire.Hosting.Tests.Ats.ThirdParty.AspireExportAttribute;
using ThirdPartyExportIgnore = Aspire.Hosting.Tests.Ats.ThirdParty.AspireExportIgnoreAttribute;
using ThirdPartyDtoAttr = Aspire.Hosting.Tests.Ats.ThirdParty.AspireDtoAttribute;
using ThirdPartyUnion = Aspire.Hosting.Tests.Ats.ThirdParty.AspireUnionAttribute;

namespace Aspire.Hosting.Tests.Ats;

[Trait("Partition", "4")]
public class AttributeDataReaderTests
{
    #region Name-Based Discovery Tests

    [Fact]
    public void GetAspireExportData_FindsOfficialAttribute_OnMethod()
    {
        // Verify that the official [AspireExport] from Aspire.Hosting namespace is discovered via name-based matching
        var method = typeof(OfficialAttributeExports).GetMethod(nameof(OfficialAttributeExports.OfficialExportMethod))!;
        var result = AttributeDataReader.GetAspireExportData(method);

        Assert.NotNull(result);
        Assert.Equal("officialMethod", result.Id);
        Assert.Equal("Official method description", result.Description);
    }

    [Fact]
    public void GetAspireExportData_FindsThirdPartyAttribute_OnMethod()
    {
        // Third-party authors define their own [AspireExport] in a different namespace.
        // The scanner should still discover it by name.
        var method = typeof(ThirdPartyExports).GetMethod(nameof(ThirdPartyExports.ThirdPartyMethod))!;
        var result = AttributeDataReader.GetAspireExportData(method);

        Assert.NotNull(result);
        Assert.Equal("thirdPartyMethod", result.Id);
        Assert.Equal("Third party method", result.Description);
    }

    [Fact]
    public void GetAspireExportData_FindsThirdPartyAttribute_OnType()
    {
        var result = AttributeDataReader.GetAspireExportData(typeof(ThirdPartyResource));

        Assert.NotNull(result);
        Assert.True(result.ExposeProperties);
    }

    [Fact]
    public void HasAspireExportIgnoreData_FindsThirdPartyAttribute()
    {
        var property = typeof(ThirdPartyResource).GetProperty(nameof(ThirdPartyResource.InternalProp))!;
        var result = AttributeDataReader.HasAspireExportIgnoreData(property);

        Assert.True(result);
    }

    [Fact]
    public void HasAspireDtoData_FindsThirdPartyAttribute()
    {
        var result = AttributeDataReader.HasAspireDtoData(typeof(ThirdPartyDtoType));

        Assert.True(result);
    }

    [Fact]
    public void GetAspireUnionData_FindsThirdPartyAttribute_OnParameter()
    {
        var method = typeof(ThirdPartyExports).GetMethod(nameof(ThirdPartyExports.ThirdPartyMethod))!;
        var param = method.GetParameters()[0];
        var result = AttributeDataReader.GetAspireUnionData(param);

        Assert.NotNull(result);
        Assert.Equal(2, result.Types.Length);
        Assert.Contains(typeof(string), result.Types);
        Assert.Contains(typeof(int), result.Types);
    }

    [Fact]
    public void GetAspireExportData_ReadsAllNamedProperties()
    {
        var method = typeof(OfficialAttributeExports).GetMethod(nameof(OfficialAttributeExports.OverriddenNameMethod))!;
        var result = AttributeDataReader.GetAspireExportData(method);

        Assert.NotNull(result);
        Assert.Equal("overriddenMethod", result.Id);
        Assert.Equal("customName", result.MethodName);
    }

    [Fact]
    public void GetAspireExportData_ExposeMethodsFlag()
    {
        var result = AttributeDataReader.GetAspireExportData(typeof(ThirdPartyMethodsResource));

        Assert.NotNull(result);
        Assert.True(result.ExposeMethods);
        Assert.False(result.ExposeProperties);
    }

    [Fact]
    public void ScanAssembly_FindsThirdPartyAttributedMethods()
    {
        // Full integration: scan the test assembly and verify that methods annotated
        // with the third-party mock [AspireExport] attribute are discovered.
        var testAssembly = typeof(AttributeDataReaderTests).Assembly;
        var hostingAssembly = typeof(DistributedApplication).Assembly;

        var result = AtsCapabilityScanner.ScanAssemblies([hostingAssembly, testAssembly]);

        // The third-party method should be discovered via name-based matching
        var thirdPartyCapability = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId.EndsWith("/thirdPartyMethod", StringComparison.Ordinal));

        Assert.NotNull(thirdPartyCapability);
        Assert.Equal("Third party method", thirdPartyCapability.Description);
    }

    [Fact]
    public void ScanAssembly_FindsBothOfficialAndThirdPartyAttributes()
    {
        var testAssembly = typeof(AttributeDataReaderTests).Assembly;
        var hostingAssembly = typeof(DistributedApplication).Assembly;

        var result = AtsCapabilityScanner.ScanAssemblies([hostingAssembly, testAssembly]);

        var officialCapability = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId.EndsWith("/officialMethod", StringComparison.Ordinal));
        var thirdPartyCapability = result.Capabilities
            .FirstOrDefault(c => c.CapabilityId.EndsWith("/thirdPartyMethod", StringComparison.Ordinal));

        Assert.NotNull(officialCapability);
        Assert.NotNull(thirdPartyCapability);
    }

    #endregion

    #region Test Types Using Official Aspire.Hosting Attributes

    public static class OfficialAttributeExports
    {
        [AspireExport("officialMethod", Description = "Official method description")]
        public static void OfficialExportMethod(IResource resource)
        {
            _ = resource;
        }

        [AspireExport("overriddenMethod", MethodName = "customName")]
        public static void OverriddenNameMethod(IResource resource)
        {
            _ = resource;
        }
    }

    #endregion

    #region Test Types Using Third-Party Mock Attributes (Different Namespace)

    public static class ThirdPartyExports
    {
        [ThirdPartyExport("thirdPartyMethod", Description = "Third party method")]
        public static void ThirdPartyMethod(
            [ThirdPartyUnion(typeof(string), typeof(int))] object value)
        {
            _ = value;
        }
    }

    [ThirdPartyExport(ExposeProperties = true)]
    public class ThirdPartyResource : Resource
    {
        public ThirdPartyResource(string name) : base(name) { }

        public string Visible { get; set; } = "";

        [ThirdPartyExportIgnore]
        public string InternalProp { get; set; } = "";
    }

    [ThirdPartyExport(ExposeMethods = true)]
    public class ThirdPartyMethodsResource : Resource
    {
        public ThirdPartyMethodsResource(string name) : base(name) { }

        public void DoSomething() { }
    }

    [ThirdPartyDtoAttr]
    public class ThirdPartyDtoType
    {
        public string Name { get; set; } = "";
    }

    #endregion
}
