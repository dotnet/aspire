// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;

namespace Aspire.Hosting;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
/// <param name="name">The name of the construct in the Aspire application model.</param>
/// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
public class AzureConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// Callback for configuring construct.
    /// </summary>
    public Action<ResourceModuleConstruct> ConfigureConstruct { get; internal set; } = configureConstruct;

    /// <summary>
    /// Gets or sets the <see cref="Azure.Provisioning.ProvisioningContext"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningContext? ProvisioningContext { get; set; }

    /// <inheritdoc/>
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var resourceModuleConstruct = new ResourceModuleConstruct(this, Name);

        ConfigureConstruct(resourceModuleConstruct);

        // WARNING: GetParameters currently returns more than one instance of the same
        //          parameter. Its the only API that gives us what we need (a list of
        //          parameters. Here we find all the distinct parameters by name and
        //          put them into a dictionary for quick lookup so we don't need to scan
        //          through the parameter enumerable each time.
        var constructParameters = resourceModuleConstruct.GetParameters();
        var distinctConstructParameters = constructParameters.DistinctBy(p => p.ResourceName);
        var distinctConstructParametersLookup = distinctConstructParameters.ToDictionary(p => p.ResourceName);

        foreach (var aspireParameter in this.Parameters)
        {
            if (distinctConstructParametersLookup.ContainsKey(aspireParameter.Key))
            {
                continue;
            }

            var constructParameter = new BicepParameter(aspireParameter.Key, typeof(string));
            resourceModuleConstruct.Add(constructParameter);
        }

        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        var moduleSourcePath = Path.Combine(generationPath, "main.bicep");

        var plan = resourceModuleConstruct.Build(ProvisioningContext);
        var compilation = plan.Compile();
        Debug.Assert(compilation.Count == 1);
        var compiledBicep = compilation.First();
        File.WriteAllText(moduleSourcePath, compiledBicep.Value);

        var moduleDestinationPath = Path.Combine(directory ?? generationPath, $"{Name}.module.bicep");
        File.Copy(moduleSourcePath, moduleDestinationPath, true);

        return new BicepTemplateFile(moduleDestinationPath, directory is null);
    }

    private string? _generatedBicep;

    /// <inheritdoc />
    public override string GetBicepTemplateString()
    {
        if (_generatedBicep is null)
        {
            var template = GetBicepTemplateFile();
            _generatedBicep = File.ReadAllText(template.Path);
        }

        return _generatedBicep;
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
    /// <param name="configureConstruct">A callback used to configure the construct resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<ResourceModuleConstruct> configureConstruct)
    {
        builder.AddAzureProvisioning();

        var resource = new AzureConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures the Azure construct resource.
    /// </summary>
    /// <typeparam name="T">Type of the CDK resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<T> ConfigureConstruct<T>(this IResourceBuilder<T> builder, Action<ResourceModuleConstruct> configure)
        where T : AzureConstructResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Resource.ConfigureConstruct += configure;
        return builder;
    }

    /// <summary>
    /// Creates a new <see cref="BicepParameter"/> in <paramref name="construct"/>, or reuses an existing bicep parameter if one with
    /// the same name already exists, that corresponds to <paramref name="parameterResourceBuilder"/>.
    /// </summary>
    /// <param name="parameterResourceBuilder">
    /// The <see cref="IResourceBuilder{ParameterResource}"/> that represents a parameter in the <see cref="Aspire.Hosting.ApplicationModel" />
    /// to get or create a corresponding <see cref="BicepParameter"/>.
    /// </param>
    /// <param name="construct">The <see cref="ResourceModuleConstruct"/> that contains the <see cref="BicepParameter"/>.</param>
    /// <param name="parameterName">The name of the parameter to be assigned.</param>
    /// <returns>
    /// The corresponding <see cref="BicepParameter"/> that was found or newly created.
    /// </returns>
    /// <remarks>
    /// This is useful when assigning a <see cref="BicepValue"/> to the value of an Aspire <see cref="ParameterResource"/>.
    /// </remarks>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
        Justification = "The 'this' arguments are mutually exclusive")]
    public static BicepParameter AsBicepParameter(this IResourceBuilder<ParameterResource> parameterResourceBuilder, ResourceModuleConstruct construct, string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(parameterResourceBuilder);
        ArgumentNullException.ThrowIfNull(construct);

        parameterName ??= parameterResourceBuilder.Resource.Name;

        construct.Resource.Parameters[parameterName] = parameterResourceBuilder.Resource;

        var parameter = construct.GetParameters().FirstOrDefault(p => p.ResourceName == parameterName);
        if (parameter is null)
        {
            parameter = new BicepParameter(parameterName, typeof(string))
            {
                IsSecure = parameterResourceBuilder.Resource.Secret
            };
            construct.Add(parameter);
        }

        return parameter;
    }

    /// <summary>
    /// Creates a new <see cref="BicepParameter"/> in <paramref name="construct"/>, or reuses an existing bicep parameter if one with
    /// the same name already exists, that corresponds to <paramref name="outputReference"/>.
    /// </summary>
    /// <param name="outputReference">
    /// The <see cref="BicepOutputReference"/> that contains the value to use for the <see cref="BicepParameter"/>.
    /// </param>
    /// <param name="construct">The <see cref="ResourceModuleConstruct"/> that contains the <see cref="BicepParameter"/>.</param>
    /// <param name="parameterName">The name of the parameter to be assigned.</param>
    /// <returns>
    /// The corresponding <see cref="BicepParameter"/> that was found or newly created.
    /// </returns>
    /// <remarks>
    /// This is useful when assigning a <see cref="BicepValue"/> to the value of an Aspire <see cref="BicepOutputReference"/>.
    /// </remarks>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
        Justification = "The 'this' arguments are mutually exclusive")]
    public static BicepParameter AsBicepParameter(this BicepOutputReference outputReference, ResourceModuleConstruct construct, string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(outputReference);
        ArgumentNullException.ThrowIfNull(construct);

        parameterName ??= outputReference.Name;

        construct.Resource.Parameters[parameterName] = outputReference;

        var parameter = construct.GetParameters().FirstOrDefault(p => p.ResourceName == parameterName);
        if (parameter is null)
        {
            parameter = new BicepParameter(parameterName, typeof(string));
            construct.Add(parameter);
        }

        return parameter;
    }
}

/// <summary>
/// An Azure Provisioning construct which represents the root Bicep module that is generated for an Azure construct resource.
/// </summary>
public class ResourceModuleConstruct : Infrastructure
{
    internal ResourceModuleConstruct(AzureConstructResource resource, string name) : base(name)
    {
        Resource = resource;

        // Always add a default location parameter.
        // azd assumes there will be a location parameter for every module.
        // The Infrastructure location resolver will resolve unset Location properties to this parameter.
        Add(new BicepParameter("location", typeof(string))
        {
            Description = "The location for the resource(s) to be deployed.",
            Value = BicepFunction.GetResourceGroup().Location
        });
    }

    /// <summary>
    /// The Azure construct resource that this resource module construct represents.
    /// </summary>
    public AzureConstructResource Resource { get; }

    /// <summary>
    /// The common principalId parameter injected into most Aspire-based Bicep files.
    /// </summary>
    public BicepParameter PrincipalIdParameter => new BicepParameter("principalId", typeof(string));

    /// <summary>
    /// The common principalType parameter injected into most Aspire-based Bicep files.
    /// </summary>
    public BicepParameter PrincipalTypeParameter => new BicepParameter("principalType", typeof(string));

    /// <summary>
    /// The common principalName parameter injected into some Aspire-based Bicep files.
    /// </summary>
    public BicepParameter PrincipalNameParameter => new BicepParameter("principalName", typeof(string));

    internal IEnumerable<BicepParameter> GetParameters() => GetResources().OfType<BicepParameter>();
}
