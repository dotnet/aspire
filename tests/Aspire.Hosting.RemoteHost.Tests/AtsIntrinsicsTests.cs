// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class AtsIntrinsicsTests
{
    // Primitive type tests
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(char))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(Uri))]
    public void IsAtsCompatible_ReturnsTrueForPrimitives(Type type)
    {
        Assert.True(AtsIntrinsics.IsAtsCompatible(type));
    }

    [Fact]
    public void IsAtsCompatible_ReturnsTrueForNullablePrimitives()
    {
        Assert.True(AtsIntrinsics.IsAtsCompatible(typeof(int?)));
        Assert.True(AtsIntrinsics.IsAtsCompatible(typeof(bool?)));
        Assert.True(AtsIntrinsics.IsAtsCompatible(typeof(DateTime?)));
        Assert.True(AtsIntrinsics.IsAtsCompatible(typeof(Guid?)));
    }

    [Fact]
    public void IsAtsCompatible_ReturnsTrueForEnums()
    {
        Assert.True(AtsIntrinsics.IsAtsCompatible(typeof(TestEnum)));
    }

    [Fact]
    public void IsAtsCompatible_ReturnsFalseForArbitraryTypes()
    {
        Assert.False(AtsIntrinsics.IsAtsCompatible(typeof(TestClass)));
        Assert.False(AtsIntrinsics.IsAtsCompatible(typeof(object)));
    }

    // Intrinsic type tests
    [Fact]
    public void GetTypeId_ReturnsTypeIdForIDistributedApplicationBuilder()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(IDistributedApplicationBuilder));

        Assert.Equal("aspire/Builder", typeId);
    }

    [Fact]
    public void GetTypeId_ReturnsTypeIdForDistributedApplication()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(DistributedApplication));

        Assert.Equal("aspire/Application", typeId);
    }

    [Fact]
    public void GetTypeId_ReturnsNullForNonIntrinsicType()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(TestClass));

        Assert.Null(typeId);
    }

    [Fact]
    public void IsIntrinsic_ReturnsTrueForIntrinsicTypes()
    {
        Assert.True(AtsIntrinsics.IsIntrinsic(typeof(IDistributedApplicationBuilder)));
        Assert.True(AtsIntrinsics.IsIntrinsic(typeof(DistributedApplication)));
    }

    [Fact]
    public void IsIntrinsic_ReturnsFalseForNonIntrinsicTypes()
    {
        Assert.False(AtsIntrinsics.IsIntrinsic(typeof(TestClass)));
        Assert.False(AtsIntrinsics.IsIntrinsic(typeof(string)));
    }

    // Resource type tests
    [Fact]
    public void GetResourceType_ReturnsResourceTypeFromIResourceBuilder()
    {
        var resourceType = AtsIntrinsics.GetResourceType(typeof(IResourceBuilder<ContainerResource>));

        Assert.Equal(typeof(ContainerResource), resourceType);
    }

    [Fact]
    public void GetResourceType_ReturnsNullForNonResourceBuilder()
    {
        var resourceType = AtsIntrinsics.GetResourceType(typeof(string));

        Assert.Null(resourceType);
    }

    [Fact]
    public void GetResourceTypeId_StripsResourceSuffix()
    {
        var typeId = AtsIntrinsics.GetResourceTypeId(typeof(ContainerResource));

        Assert.Equal("aspire/Container", typeId);
    }

    [Fact]
    public void IsResourceBuilder_ReturnsTrueForResourceBuilder()
    {
        Assert.True(AtsIntrinsics.IsResourceBuilder(typeof(IResourceBuilder<ContainerResource>)));
    }

    [Fact]
    public void IsResourceBuilder_ReturnsFalseForNonResourceBuilder()
    {
        Assert.False(AtsIntrinsics.IsResourceBuilder(typeof(string)));
        Assert.False(AtsIntrinsics.IsResourceBuilder(typeof(IDistributedApplicationBuilder)));
    }

    [Fact]
    public void IsResourceAssignableTo_ReturnsTrueForSameType()
    {
        Assert.True(AtsIntrinsics.IsResourceAssignableTo(typeof(ContainerResource), typeof(ContainerResource)));
    }

    [Fact]
    public void IsResourceAssignableTo_ReturnsTrueForDerivedType()
    {
        // ContainerResource implements IResource
        Assert.True(AtsIntrinsics.IsResourceAssignableTo(typeof(ContainerResource), typeof(IResource)));
    }

    [Fact]
    public void IsResourceAssignableTo_ReturnsFalseForUnrelatedTypes()
    {
        Assert.False(AtsIntrinsics.IsResourceAssignableTo(typeof(ContainerResource), typeof(string)));
    }

    // GetTypeId for IResource implementations
    [Fact]
    public void GetTypeId_ReturnsTypeIdForIResourceImplementation()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(ContainerResource));

        Assert.Equal("aspire/Container", typeId);
    }

    [Fact]
    public void GetTypeId_ReturnsTypeIdForIResourceBuilder()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(IResourceBuilder<ContainerResource>));

        Assert.Equal("aspire/Container", typeId);
    }

    // Interface type mapping tests
    [Fact]
    public void GetTypeId_ReturnsTypeIdForIResource()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(IResource));

        Assert.Equal("aspire/IResource", typeId);
    }

    [Fact]
    public void GetTypeId_ReturnsTypeIdForIResourceWithEnvironment()
    {
        var typeId = AtsIntrinsics.GetTypeId(typeof(IResourceWithEnvironment));

        Assert.Equal("aspire/IResourceWithEnvironment", typeId);
    }

    private enum TestEnum { A, B }
    private sealed class TestClass { }
}
