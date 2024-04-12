// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class CDKProvisionerContext(DistributedApplicationModel model, ResourceNotificationService notificationService)
{

    public DistributedApplicationModel AppModel { get; } = model;

    public IEnumerable<StackResource> StackResources { get; } = model.Resources.OfType<StackResource>();

    public IEnumerable<ConstructResource> ConstructResources { get; } = model.Resources.OfType<ConstructResource>();

    private Lazy<IImmutableDictionary<IStackResource, IEnumerable<IConstructResource>>> ConstructResourcesInStack { get; } = new(model.Resources.GetResourcesGroupedByParent<IStackResource, IConstructResource>());

    public async Task PublishUpdateStateAsync(IStackResource resource, string status, ImmutableArray<ResourcePropertySnapshot>? properties = null)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<ResourcePropertySnapshot>();
        }

        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = GetResourceType<Stack>(resource),
            State = status,
            Properties = state.Properties.AddRange(properties)
        }).ConfigureAwait(false);
        if (ConstructResourcesInStack.Value.TryGetValue(resource, out var constructResources))
        {
            foreach (var constructResource in constructResources)
            {
                await notificationService
                    .PublishUpdateAsync(constructResource,
                        state => state with { ResourceType = GetResourceType<Construct>(constructResource), State = status })
                    .ConfigureAwait(false);
            }
        }
    }

    private static string GetResourceType<T>(IResourceWithConstruct constructResource)
        where T : Construct
    {
        var constructType = constructResource.Construct.GetType();
        var baseConstructType = typeof(T);
        return constructType == baseConstructType ? baseConstructType.Name : $"{constructType.Name}({baseConstructType.Name})";
    }
}
