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
/// <param name="configureConstruct"></param>
public class AzureConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// TODO:
    /// </summary>
    public Action<ResourceModuleConstruct> ConfigureConstruct { get; } = configureConstruct;

    /// <inheritdoc />
    public override void WriteToManifest(ManifestPublishingContext context)
    {
        var resourceModuleConstruct = new ResourceModuleConstruct(this);

        var locationParameter = new Parameter("location", defaultValue: "West US 3");
        resourceModuleConstruct.AddParameter(locationParameter);

        foreach (var aspireParameter in this.Parameters)
        {
            var constructParameter = new Parameter(aspireParameter.Key);
            resourceModuleConstruct.AddParameter(constructParameter);
        }

        ConfigureConstruct(resourceModuleConstruct);

        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        resourceModuleConstruct.Build(generationPath);

        var moduleSourcePath = Path.Combine(generationPath, "main.bicep");
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
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Action<ResourceModuleConstruct> configureConstruct)
    {
        var resource = new AzureConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="resourceModuleConstruct"></param>
    /// <param name="parameterResourceBuilder"></param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        return resourceModuleConstruct.AddParameter(parameterResourceBuilder.Resource.Name, parameterResourceBuilder);
    }

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="resourceModuleConstruct"></param>
    /// <param name="name"></param>
    /// <param name="parameterResourceBuilder"></param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, string name, IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        // Ensure the parameter is added to the Aspire resource.
        resourceModuleConstruct.Resource.Parameters.Add(name, parameterResourceBuilder);

        var parameter = new Parameter(name, isSecure: parameterResourceBuilder.Resource.Secret);
        resourceModuleConstruct.AddParameter(parameter);
        return parameter;
    }

    /// <summary>
    ///  TODO:
    /// </summary>
    /// <param name="resourceModuleConstruct"></param>
    /// <param name="outputReference"></param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, BicepOutputReference outputReference)
    {
        return resourceModuleConstruct.AddParameter(outputReference.Name, outputReference);
    }

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="resourceModuleConstruct"></param>
    /// <param name="name"></param>
    /// <param name="outputReference"></param>
    /// <returns></returns>
    public static Parameter AddParameter(this ResourceModuleConstruct resourceModuleConstruct, string name, BicepOutputReference outputReference)
    {
        resourceModuleConstruct.Resource.Parameters.Add(name, outputReference);

        var parameter = new Parameter(name);
        resourceModuleConstruct.AddParameter(parameter);
        return parameter;
    }
}

/// <summary>
/// TODO: Can't think of a better name right now
/// </summary>
public class ResourceModuleConstruct : Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    public ResourceModuleConstruct(AzureConstructResource resource) : base(constructScope: ConstructScope.ResourceGroup, tenantId: Guid.Empty, subscriptionId: Guid.Empty, envName: "temp", useAnonymousResourceGroup: true)
    {
        Resource = resource;
        LocationParameter = new Parameter("location", "West US 3");
        AddParameter(LocationParameter);
    }

    /// <summary>
    /// TODO:
    /// </summary>
    public AzureConstructResource Resource { get; }

    /// <summary>
    /// TODO:
    /// </summary>
    public Parameter LocationParameter { get; }
}
