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
/// <param name="infrastructure"></param>
/// <param name="createConstruct"></param>
public class AzureConstructResource(string name, Infrastructure infrastructure, Func<IConstruct, AspireResourceConstruct> createConstruct) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// TODO:
    /// </summary>
    public Func<IConstruct, AspireResourceConstruct> CreateConstruct { get; } = createConstruct;

    /// <inheritdoc />
    public override void WriteToManifest(ManifestPublishingContext context)
    {
        // HACK: Using CDK to generate files but then copying just the module
        //       to where it needs to be.
        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        CreateConstruct(infrastructure);
        infrastructure.Build(generationPath);

        var moduleSourcePath = Path.Combine(generationPath, "resources", "rg_temp_module", "rg_temp_module.bicep");
        var moduleDestinationPath = context.GetManifestRelativePath(TemplateFile);
        File.Copy(moduleSourcePath, moduleDestinationPath!, true);

        Directory.Delete(generationPath, true);

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
        var createConstruct = (IConstruct subscriptionConstruct) =>
        {
            var resourceConstruct = new AspireResourceConstruct(subscriptionConstruct, name, builder.Environment.EnvironmentName);
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
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Func<IConstruct, AspireResourceConstruct> createConstruct)
    {
        // HACK: We shouldn't need this.
        var infrastructure = new DummyInfrastructure(Guid.NewGuid(), Guid.NewGuid(), "temp");

        var resource = new AzureConstructResource(name, infrastructure, createConstruct);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

/// <summary>
/// TODO:
/// </summary>
/// <param name="scope"></param>
/// <param name="resourceName"></param>
/// <param name="envName"></param>
public class AspireResourceConstruct(IConstruct scope, string resourceName, string envName) : Construct(scope, resourceName, ConstructScope.ResourceGroup, tenantId: Guid.NewGuid(), subscriptionId: Guid.NewGuid(), envName: envName)
{
}

internal class DummyInfrastructure(Guid tenantId, Guid subscriptionId, string envName) : Infrastructure(tenantId: tenantId, subscriptionId: subscriptionId, envName: envName)
{
}
