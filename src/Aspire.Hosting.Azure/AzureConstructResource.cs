// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Publishing;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// An Aspire resource which is also a CDK construct.
/// </summary>
/// <param name="name"></param>
/// <param name="createConstruct"></param>
public class AzureConstructResource(string name, Func<AspireResourceConstruct> createConstruct) : AzureBicepResource(name, templateFile: $"{name}.generated.bicep")
{
    /// <summary>
    /// TODO:
    /// </summary>
    public Func<AspireResourceConstruct> CreateConstruct { get; } = createConstruct;

    /// <inheritdoc />
    public override void WriteToManifest(ManifestPublishingContext context)
    {
        var path = context.GetManifestRelativePath($"{Name}.generated.bicep");
        var construct = CreateConstruct();
        construct.Build(path);

        base.WriteToManifest(context);
    }
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
    /// <param name="configureConstruct">A callback used to configure the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Action<IConstruct> configureConstruct)
    {
        var createConstruct = () =>
        {
            var resourceConstruct = new AspireResourceConstruct(builder.Environment.EnvironmentName);
            configureConstruct(resourceConstruct);
            return resourceConstruct;
        };
        return builder.AddAzureConstruct(name, createConstruct);
    }

    /// <summary>
    /// Adds a CDK resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource being added.</param>
    /// <param name="createConstruct"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Func<AspireResourceConstruct> createConstruct)
    {
        var resource = new AzureConstructResource(name, createConstruct);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

/// <summary>
/// TODO:
/// </summary>
/// <param name="envName"></param>
public class AspireResourceConstruct(string envName) : Infrastructure(ConstructScope.ResourceGroup, envName: envName)
{
}
