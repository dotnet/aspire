// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.ResourceGraph;
using Aspire.Dashboard.Resources;
using Aspire.Tests.Shared.DashboardModel;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public class ResourceGraphMapperTests
{
    [Fact]
    public void MapResource_HasReference_Added()
    {
        // Arrange
        var resource1 = ModelTestHelpers.CreateResource("app1-abcxyc", displayName: "app1", relationships: [new RelationshipViewModel("app2", "Reference")]);
        var resource2 = ModelTestHelpers.CreateResource("app2-123456", displayName: "app2", relationships: ImmutableArray<RelationshipViewModel>.Empty);
        var resources = new Dictionary<string, ResourceViewModel>
        {
            [resource1.Name] = resource1,
            [resource2.Name] = resource2,
        };

        // Act
        var dto = ResourceGraphMapper.MapResource(resource1, resources, new TestStringLocalizer<Columns>(), showHiddenResources: false);

        // Assert
        var referencedName = Assert.Single(dto.ReferencedNames);
        Assert.Equal("app2-123456", referencedName);
    }

    [Fact]
    public void MapResource_HasReferenceToReplicas_MultipleAdded()
    {
        // Arrange
        var resource1 = ModelTestHelpers.CreateResource("app1-abcxyc", displayName: "app1", relationships: [new RelationshipViewModel("app2", "Reference")]);
        var resource21 = ModelTestHelpers.CreateResource("app2-123456", displayName: "app2", relationships: ImmutableArray<RelationshipViewModel>.Empty);
        var resource22 = ModelTestHelpers.CreateResource("app2-654321", displayName: "app2", relationships: ImmutableArray<RelationshipViewModel>.Empty);
        var resources = new Dictionary<string, ResourceViewModel>
        {
            [resource1.Name] = resource1,
            [resource21.Name] = resource21,
            [resource22.Name] = resource22,
        };

        // Act
        var dto = ResourceGraphMapper.MapResource(resource1, resources, new TestStringLocalizer<Columns>(), showHiddenResources: false);

        // Assert
        Assert.Collection(dto.ReferencedNames,
            r => Assert.Equal("app2-123456", r),
            r => Assert.Equal("app2-654321", r));
    }

    [Fact]
    public void MapResource_HasSelfReference_Ignored()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource("app1-abcxyc", displayName: "app1", relationships: ImmutableArray<RelationshipViewModel>.Empty);
        var resources = new Dictionary<string, ResourceViewModel>
        {
            [resource.Name] = resource,
        };

        // Act
        var dto = ResourceGraphMapper.MapResource(resource, resources, new TestStringLocalizer<Columns>(), showHiddenResources: false);

        // Assert
        Assert.Empty(dto.ReferencedNames);
    }

    [Fact]
    public void MapResource_ShowHiddenResources_IncludesHiddenResources()
    {
        // Arrange
        var resource1 = ModelTestHelpers.CreateResource("app1-abcxyc", displayName: "app1", relationships: [new RelationshipViewModel("hidden-app", "Reference")]);
        var hiddenResource = ModelTestHelpers.CreateResource("hidden-app", displayName: "hidden-app", relationships: ImmutableArray<RelationshipViewModel>.Empty, hidden: true);
        var resources = new Dictionary<string, ResourceViewModel>
        {
            [resource1.Name] = resource1,
            [hiddenResource.Name] = hiddenResource,
        };

        // Act
        var dto = ResourceGraphMapper.MapResource(resource1, resources, new TestStringLocalizer<Columns>(), showHiddenResources: true);

        // Assert
        Assert.Contains("hidden-app", dto.ReferencedNames);
    }
}
