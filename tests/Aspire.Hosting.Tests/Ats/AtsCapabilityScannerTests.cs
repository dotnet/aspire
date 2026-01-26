// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;

namespace Aspire.Hosting.Tests.Ats;

public class AtsCapabilityScannerTests
{
    #region MapToAtsTypeId Tests

    [Fact]
    public void MapToAtsTypeId_String_ReturnsString()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(string));

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToAtsTypeId_Int32_ReturnsNumber()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(int));

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_Boolean_ReturnsBoolean()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(bool));

        Assert.Equal("boolean", result);
    }

    [Fact]
    public void MapToAtsTypeId_Void_ReturnsNull()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(void));

        Assert.Null(result);
    }

    [Fact]
    public void MapToAtsTypeId_Task_ReturnsNull()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(Task));

        Assert.Null(result);
    }

    [Fact]
    public void MapToAtsTypeId_TaskOfString_ReturnsString()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(Task<string>));

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToAtsTypeId_TaskOfInt_ReturnsNumber()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(Task<int>));

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_NullableInt_ReturnsNumber()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(int?));

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_StringArray_ReturnsStringArray()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(string[]));

        Assert.Equal("string[]", result);
    }

    [Fact]
    public void MapToAtsTypeId_IntArray_ReturnsNumberArray()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(int[]));

        Assert.Equal("number[]", result);
    }

    [Fact]
    public void MapToAtsTypeId_IResourceBuilder_ExtractsResourceType()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(IResourceBuilder<TestResource>));

        // Should derive type ID from TestResource's full name
        // Format: {AssemblyName}/{FullTypeName}
        Assert.Equal("Aspire.Hosting.Tests/Aspire.Hosting.Tests.Ats.AtsCapabilityScannerTests+TestResource", result);
    }

    [Fact]
    public void MapToAtsTypeId_UnknownType_ReturnsNull()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(AtsCapabilityScannerTests));

        // Unknown types return null (capabilities with unknown types are skipped)
        Assert.Null(result);
    }

    [Fact]
    public void MapToAtsTypeId_ObjectType_ReturnsAny()
    {
        var result = AtsCapabilityScanner.MapToAtsTypeId(typeof(object));

        // System.Object maps to 'any'
        Assert.Equal("any", result);
    }

    #endregion

    #region DeriveMethodName Tests

    [Fact]
    public void DeriveMethodName_SimpleCapabilityId_ReturnsMethodName()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("Aspire.Hosting/createBuilder");

        Assert.Equal("createBuilder", result);
    }

    [Fact]
    public void DeriveMethodName_NestedCapabilityId_ReturnsMethodName()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("Aspire.Hosting.Redis/addRedis");

        Assert.Equal("addRedis", result);
    }

    [Fact]
    public void DeriveMethodName_NoSlash_ReturnsEntireId()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("withEnvironment");

        Assert.Equal("withEnvironment", result);
    }

    #endregion

    #region DerivePackage Tests

    [Fact]
    public void DerivePackage_SimpleCapabilityId_ReturnsPackage()
    {
        var result = AtsCapabilityScanner.DerivePackage("Aspire.Hosting/createBuilder");

        Assert.Equal("Aspire.Hosting", result);
    }

    [Fact]
    public void DerivePackage_NestedCapabilityId_ReturnsPackage()
    {
        var result = AtsCapabilityScanner.DerivePackage("Aspire.Hosting.Redis/addRedis");

        Assert.Equal("Aspire.Hosting.Redis", result);
    }

    #endregion

    #region Test Types

    private sealed class TestResource : Resource
    {
        public TestResource(string name) : base(name)
        {
        }
    }

    #endregion
}
