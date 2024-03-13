// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

namespace Aspire.Hosting;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
/// <param name="name"></param>
public class AzureConstructResource(string name) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    // /// <summary>
    /// Callback for configuring construct.
    // /// </summary>
    internal Action<ResourceModuleConstruct>? ConfigureConstruct { get; set; }

    /// <inheritdoc/>
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var configuration = new Configuration()
        {
            UseInteractiveMode = true
        };

        var resourceModuleConstruct = new ResourceModuleConstruct(this, configuration);

        // ConfigureConstruct(resourceModuleConstruct);

        // WARNING: GetParameters currently returns more than one instance of the same
        //          parameter. Its the only API that gives us what we need (a list of
        //          parameters. Here we find all the distinct parameters by name and
        //          put them into a dictionary for quick lookup so we don't need to scan
        //          through the parameter enumerable each time.
        var constructParameters = resourceModuleConstruct.GetParameters();
        var distinctConstructParameters = constructParameters.DistinctBy(p => p.Name);
        var distinctConstructParametersLookup = distinctConstructParameters.ToDictionary(p => p.Name);

        foreach (var aspireParameter in this.Parameters)
        {
            if (distinctConstructParametersLookup.ContainsKey(aspireParameter.Key))
            {
                continue;
            }

            var constructParameter = new Parameter(aspireParameter.Key);
            resourceModuleConstruct.AddParameter(constructParameter);
        }

        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        resourceModuleConstruct.Build(generationPath);

        var moduleSourcePath = Path.Combine(generationPath, "main.bicep");
        var moduleDestinationPath = Path.Combine(directory ?? generationPath, $"{Name}.module.bicep");
        File.Copy(moduleSourcePath, moduleDestinationPath!, true);

        return new BicepTemplateFile(moduleDestinationPath, directory is null);
    }
}

/// <summary>
/// Extensions for working with <see cref="AzureConstructResource"/> and related types.
/// </summary>
public static class AzureConstructResourceExtensions
{
    /// <summary>
    /// Adds an Azure construct resource to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource being added.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureConstructResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Assigns an Aspire parameter resource to an Azure construct resource.
    /// </summary>
    /// <typeparam name="T">Type of the CDK resource.</typeparam>
    /// <param name="resource">The CDK resource.</param>
    /// <param name="propertySelector">Property selection expression.</param>
    /// <param name="parameterResourceBuilder">Aspire parameter resource builder.</param>
    /// <param name="parameterName">The name of the parameter to be assigned.</param>
    public static void AssignProperty<T>(this Resource<T> resource, Expression<Func<T, object?>> propertySelector, IResourceBuilder<ParameterResource> parameterResourceBuilder, string? parameterName = null) where T: notnull
    {
        parameterName ??= parameterResourceBuilder.Resource.Name;

        if (resource.Scope is not ResourceModuleConstruct construct)
        {
            throw new ArgumentException("Cannot bind Aspire parameter resource to this construct.", nameof(resource));
        }

        construct.Resource.Parameters[parameterName] = parameterResourceBuilder.Resource;

        if (resource.Scope.GetParameters().Any(p => p.Name == parameterName))
        {
            var parameter = resource.Scope.GetParameters().Single(p => p.Name == parameterName);
            resource.AssignProperty(propertySelector, parameter.Name);
        }
        else
        {
            var parameter = new Parameter(parameterName, isSecure: parameterResourceBuilder.Resource.Secret);
            resource.AssignProperty(propertySelector, parameter);
        }
    }

    /// <summary>
    /// Assigns an Aspire Bicep output reference to an Azure construct resource.
    /// </summary>
    /// <typeparam name="T">Type of the CDK resource.</typeparam>
    /// <param name="resource">The CDK resource.</param>
    /// <param name="propertySelector">Property selection expression.</param>
    /// <param name="parameterName">The name of the parameter to be assigned.</param>
    /// <param name="outputReference">Aspire parameter resource builder.</param>
    public static void AssignProperty<T>(this Resource<T> resource, Expression<Func<T, object?>> propertySelector, BicepOutputReference outputReference, string? parameterName = null) where T : notnull
    {
        parameterName ??= outputReference.Resource.Name;

        if (resource.Scope is not ResourceModuleConstruct construct)
        {
            throw new ArgumentException("Cannot bind Aspire parameter resource to this construct.", nameof(resource));
        }

        construct.Resource.Parameters[parameterName] = outputReference;

        if (resource.Scope.GetParameters().Any(p => p.Name == parameterName))
        {
            var parameter = resource.Scope.GetParameters().Single(p => p.Name == parameterName);
            resource.AssignProperty(propertySelector, parameter);
        }
        else
        {
            var parameter = new Parameter(parameterName);
            resource.AssignProperty(propertySelector, parameter);
        }
    }
}

/// <summary>
/// An Azure Provisioning construct which represents the root Bicep module that is generated for an Azure construct resource.
/// </summary>
public class ResourceModuleConstruct : Infrastructure
{
    internal ResourceModuleConstruct(AzureConstructResource resource, Configuration configuration) : base(constructScope: ConstructScope.ResourceGroup, tenantId: Guid.Empty, subscriptionId: Guid.Empty, envName: "temp", configuration: configuration)
    {
        Resource = resource;
    }

    /// <summary>
    /// The Azure cosntruct resource that this resource module construct represents.
    /// </summary>
    public AzureConstructResource Resource { get; }

    /// <summary>
    /// TODO:
    /// </summary>
    public Parameter PrincipalIdParameter => new Parameter("principalId");

    /// <summary>
    /// TODO:
    /// </summary>
    public Parameter PrincipalTypeParameter => new Parameter("principalType");

    /// <summary>
    /// TODO:
    /// </summary>
    public Parameter PrincipalNameParameter => new Parameter("principalName");
}
