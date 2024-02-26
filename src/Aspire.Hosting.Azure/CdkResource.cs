// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.ResourceManager;
using AspireResource = Aspire.Hosting.ApplicationModel.Resource;

namespace Aspire.Hosting;

/// <summary>
/// An Aspire resource which is also a CDK construct.
/// </summary>
/// <param name="name"></param>
public class CdkResource(string name) : AspireResource(name)
{

}

/// <summary>
/// TODO:
/// </summary>
/// <param name="name"></param>
public class AzureStorageCdkResource(string name) : CdkResource(name)
{

}

/// <summary>
/// Extensions for working with CDK resources in the .NET Aspire application model.
/// </summary>
public static class CdkResourceExtensions
{
    /// <summary>
    /// Adds a CDK resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource being added.</param>
    /// <param name="callback">A callback used to configure the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<CdkResource> AddCdkResource(this IDistributedApplicationBuilder builder, string name, Action<AspireResourceConstruct>? callback = null)
    {
        builder.Services.TryAddLifecycleHook<CdkLifecycleHook>();

        callback ??= static (construct) => { };

        var resource = new CdkResource(name);
        return builder.AddResource(resource)
                      .WithAnnotation(new AspireResourceConstructCallbackAnnotation(callback), ResourceAnnotationMutationBehavior.Replace);
    }
}

/// <summary>
/// An Azure.Provisioning construct which maps to an Aspire Resource.
/// </summary>
public class AspireResourceConstruct(IConstruct? scope, string name, ConstructScope constructScope = ConstructScope.ResourceGroup, Guid? tenantId = null, Guid? subscriptionId = null, string? envName = null, ResourceGroup? resourceGroup = null) : Construct(scope, name, constructScope, tenantId, subscriptionId, envName, resourceGroup)
{
}

internal class AspireInfrastructureConstruct(string envName) : Infrastructure(envName: envName)
{
}

/// <summary>
/// TODO: Doc comments.
/// </summary>
public class AspireResourceConstructCallbackAnnotation(Action<AspireResourceConstruct> callback) : IResourceAnnotation
{
    /// <summary>
    /// TODO: Doc comments.
    /// </summary>
    public Action<AspireResourceConstruct> Callback = callback;
}

internal class CdkLifecycleHook(DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        var cdkResources = appModel.Resources.OfType<CdkResource>();
        var infrastructure = new AspireInfrastructureConstruct("test");

        // Firstly suppress individual publishing of all CDK resources to the manifest.
        foreach (var resource in cdkResources)
        {
            if (resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().SingleOrDefault() is { } existingAnnotation)
            {
                resource.Annotations.Remove(existingAnnotation);
            }

            resource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

            var resourceConstruct = new AspireResourceConstruct(infrastructure, resource.Name);
            var callbackAnnotation = resource.Annotations.OfType<AspireResourceConstructCallbackAnnotation>().Single();
            callbackAnnotation.Callback(resourceConstruct);
        }

        var path = Path.GetTempFileName();
        infrastructure.Build(path);
        // TODO: Insert code that spits out a Bicep resource to the manifest and generates
        //       the Bicep based on the CDK resources?

        return Task.CompletedTask;
    }

}
