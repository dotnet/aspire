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
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(string));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToAtsTypeId_Int32_ReturnsNumber()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(int));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_Boolean_ReturnsBoolean()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(bool));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("boolean", result);
    }

    [Fact]
    public void MapToAtsTypeId_Void_ReturnsNull()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(void));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Null(result);
    }

    [Fact]
    public void MapToAtsTypeId_Task_ReturnsNull()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(Task));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Null(result);
    }

    [Fact]
    public void MapToAtsTypeId_TaskOfString_ReturnsString()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(Task<string>));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToAtsTypeId_TaskOfInt_ReturnsNumber()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(Task<int>));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_NullableInt_ReturnsNumber()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(int?));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("number", result);
    }

    [Fact]
    public void MapToAtsTypeId_StringArray_ReturnsStringArray()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(string[]));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("string[]", result);
    }

    [Fact]
    public void MapToAtsTypeId_IntArray_ReturnsNumberArray()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(int[]));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("number[]", result);
    }

    [Fact]
    public void MapToAtsTypeId_IResourceBuilder_ExtractsResourceType()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(IResourceBuilder<TestResource>));

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        // Should infer aspire/Test from TestResource
        Assert.Equal("aspire/Test", result);
    }

    [Fact]
    public void MapToAtsTypeId_UnknownType_ReturnsAny()
    {
        var typeMapping = AtsTypeMapping.Empty;
        var typeInfo = new RuntimeTypeInfo(typeof(AtsCapabilityScannerTests)); // Not a known type

        var result = AtsCapabilityScanner.MapToAtsTypeId(typeInfo, typeMapping, typeResolver: null);

        Assert.Equal("any", result);
    }

    #endregion

    #region DeriveMethodName Tests

    [Fact]
    public void DeriveMethodName_SimpleCapabilityId_ReturnsMethodName()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("aspire/createBuilder@1");

        Assert.Equal("createBuilder", result);
    }

    [Fact]
    public void DeriveMethodName_NestedCapabilityId_ReturnsMethodName()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("aspire.redis/addRedis@1");

        Assert.Equal("addRedis", result);
    }

    [Fact]
    public void DeriveMethodName_NoVersion_ReturnsMethodName()
    {
        var result = AtsCapabilityScanner.DeriveMethodName("aspire/withEnvironment");

        Assert.Equal("withEnvironment", result);
    }

    #endregion

    #region DerivePackage Tests

    [Fact]
    public void DerivePackage_SimpleCapabilityId_ReturnsPackage()
    {
        var result = AtsCapabilityScanner.DerivePackage("aspire/createBuilder@1");

        Assert.Equal("aspire", result);
    }

    [Fact]
    public void DerivePackage_NestedCapabilityId_ReturnsPackage()
    {
        var result = AtsCapabilityScanner.DerivePackage("aspire.redis/addRedis@1");

        Assert.Equal("aspire.redis", result);
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
