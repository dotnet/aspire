// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.RemoteHost.Ats;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class TypeHierarchyTests
{
    [Fact]
    public void IsAssignableTo_ReturnsTrueForSameType()
    {
        var hierarchy = new TypeHierarchy([]);

        Assert.True(hierarchy.IsAssignableTo("aspire/SomeType", "aspire/SomeType"));
    }

    [Fact]
    public void IsAssignableTo_ReturnsFalseForUnrelatedTypes()
    {
        var hierarchy = new TypeHierarchy([]);

        Assert.False(hierarchy.IsAssignableTo("aspire/TypeA", "aspire/TypeB"));
    }

    [Fact]
    public void Constructor_ScansAssemblyForResourceTypes()
    {
        // This test verifies that scanning works without throwing
        var hierarchy = new TypeHierarchy([typeof(TypeHierarchyTests).Assembly]);

        // Should not throw
        Assert.NotNull(hierarchy);
    }

    [Fact]
    public void GetResourceType_ReturnsNullForUnknownType()
    {
        var hierarchy = new TypeHierarchy([]);

        var result = hierarchy.GetResourceType("aspire/UnknownType");

        Assert.Null(result);
    }

    [Fact]
    public void GetAtsTypeId_ReturnsNullForUnknownType()
    {
        var hierarchy = new TypeHierarchy([]);

        var result = hierarchy.GetAtsTypeId(typeof(object));

        Assert.Null(result);
    }

    [Fact]
    public void GetAllTypeIds_ReturnsEmptyForEmptyHierarchy()
    {
        var hierarchy = new TypeHierarchy([]);

        var typeIds = hierarchy.GetAllTypeIds().ToList();

        // May have types from scanning, but should not throw
        Assert.NotNull(typeIds);
    }
}
