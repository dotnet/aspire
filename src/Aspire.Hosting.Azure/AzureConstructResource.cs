// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics;
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

        var plan = resourceModuleConstruct.Build();
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
    public static IResourceBuilder<AzureConstructResource> AddAzureConstruct(this IDistributedApplicationBuilder builder, string name, Action<ResourceModuleConstruct> configureConstruct)
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
    /// TODO
    /// </summary>
    /// <param name="parameterResourceBuilder"></param>
    /// <param name="construct"></param>
    /// <param name="parameterName"></param>
    /// <returns></returns>
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
    /// TODO: we don't want this in our public API.
    /// either make it shared source, or have the CDK do this for us
    /// </summary>
    /// <param name="construct"></param>
    /// <returns></returns>
    public static BicepParameter AddLocationParameter(this ResourceModuleConstruct construct)
    {
        var locationParam = new BicepParameter("location", typeof(string))
        {
            Value = BicepFunction.GetResourceGroup().Location,
        };
        construct.Add(locationParam);
        return locationParam;
    }

    /// <summary>
    /// Assigns an Aspire parameter resource to an Azure construct resource.
    /// </summary>
    /// <typeparam name="T">Type of the CDK resource.</typeparam>
    /// <param name="resource">The CDK resource.</param>
    /// <param name="propertySelector">Property selection expression.</param>
    /// <param name="parameterResourceBuilder">Aspire parameter resource builder.</param>
    /// <param name="parameterName">The name of the parameter to be assigned.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "<Pending>")]
    [Obsolete("This API is no longer supported and always throws.")]
    public static void AssignProperty<T>(this global::Azure.Provisioning.Primitives.Resource resource, System.Linq.Expressions.Expression<Func<T, object?>> propertySelector, IResourceBuilder<ParameterResource> parameterResourceBuilder, string? parameterName = null) where T : notnull
    {
        throw new NotSupportedException("");
    }

    ///// <summary>
    ///// Assigns an Aspire Bicep output reference to an Azure construct resource.
    ///// </summary>
    ///// <typeparam name="T">Type of the CDK resource.</typeparam>
    ///// <param name="resource">The CDK resource.</param>
    ///// <param name="propertySelector">Property selection expression.</param>
    ///// <param name="parameterName">The name of the parameter to be assigned.</param>
    ///// <param name="outputReference">Aspire parameter resource builder.</param>
    //[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "<Pending>")]
    //public static void AssignProperty<T>(this Resource resource, Expression<Func<T, object?>> propertySelector, BicepOutputReference outputReference, string? parameterName = null) where T : notnull
    //{
    //    parameterName ??= outputReference.Resource.Name;

    //    if (resource.Scope is not ResourceModuleConstruct construct)
    //    {
    //        throw new ArgumentException("Cannot bind Aspire parameter resource to this construct.", nameof(resource));
    //    }

    //    construct.Resource.Parameters[parameterName] = outputReference;

    //    if (resource.Scope.GetParameters().Any(p => p.Name == parameterName))
    //    {
    //        var parameter = resource.Scope.GetParameters().Single(p => p.Name == parameterName);
    //        resource.AssignProperty(propertySelector, parameter);
    //    }
    //    else
    //    {
    //        var parameter = new BicepParameter(parameterName, typeof(string));
    //        resource.AssignProperty(propertySelector, parameter);
    //    }
    //}
}

/// <summary>
/// An Azure Provisioning construct which represents the root Bicep module that is generated for an Azure construct resource.
/// </summary>
public class ResourceModuleConstruct : Infrastructure
{
    internal ResourceModuleConstruct(AzureConstructResource resource, string name) : base(name)
    {
        Resource = resource;
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

    /// <inheritdoc />
    protected override IEnumerable<Statement> Compile(ProvisioningContext? context = null)
    {
        var statements = base.Compile(context).ToList();

        int parameterCount = 0;
        int outputCount = 0;
        // we are done once we know all the remaining statements are outputs
        for (var i = 0; i < statements.Count - outputCount;)
        {
            // sort the parameters so they appear at the top of the bicep
            if (statements[i] is ParameterStatement parameterStatement)
            {
                statements.RemoveAt(i);
                statements.Insert(parameterCount, parameterStatement);
                parameterCount++;
            }
            // ensure that outputs are at the bottom of the bicep
            else if (statements[i] is OutputStatement outputStatement)
            {
                statements.RemoveAt(i);
                statements.Add(outputStatement);
                outputCount++;

                // continue and don't increment 'i', so we see the next statement that just moved into this index
                continue;
            }
            i++;
        }

        return statements;
    }
}
