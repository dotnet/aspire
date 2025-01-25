// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
public class AzureProvisioningResource : AzureBicepResource
{
    /// <summary>
    /// Callback for configuring the Azure resources.
    /// </summary>
    public Action<AzureResourceInfrastructure> ConfigureInfrastructure { get; }

    /// <summary>
    /// Gets or sets the <see cref="global::Azure.Provisioning.ProvisioningBuildOptions"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningBuildOptions? ProvisioningBuildOptions { get; set; }

    /// <inheritdoc/>
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var infrastructure = new AzureResourceInfrastructure(this, Name);

        ConfigureInfrastructure(infrastructure);

        EnsureParametersAlign(infrastructure);

        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        var moduleSourcePath = Path.Combine(generationPath, "main.bicep");

        var plan = infrastructure.Build(ProvisioningBuildOptions);
        var compilation = plan.Compile();
        Debug.Assert(compilation.Count == 1);
        var compiledBicep = compilation.First();
        File.WriteAllText(moduleSourcePath, compiledBicep.Value);

        var moduleDestinationPath = Path.Combine(directory ?? generationPath, $"{Name}.module.bicep");
        File.Copy(moduleSourcePath, moduleDestinationPath, true);

        return new BicepTemplateFile(moduleDestinationPath, directory is null);
    }

    private string? _generatedBicep;

    /// <param name="name">The name of the resource in the Aspire application model.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    public AzureProvisioningResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) : base(name, templateFile: $"{name}.module.bicep")
    {
        Annotations.Add(new AzureConfigureInfrastructureAnnotation(configureInfrastructure));

        ConfigureInfrastructure = static (infra) =>
        {
            if (infra.AspireResource.TryGetAnnotationsOfType<AzureConfigureInfrastructureAnnotation>(out var annotations))
            {
                foreach (var c in annotations)
                {
                    c.ConfigureInfrastructure(infra);
                }
            }
        };
    }

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

    private void EnsureParametersAlign(AzureResourceInfrastructure infrastructure)
    {
        // WARNING: GetParameters currently returns more than one instance of the same
        //          parameter. Its the only API that gives us what we need (a list of
        //          parameters. Here we find all the distinct parameters by name and
        //          put them into a dictionary for quick lookup so we don't need to scan
        //          through the parameter enumerable each time.
        var infrastructureParameters = infrastructure.GetParameters();
        var distinctInfrastructureParameters = infrastructureParameters.DistinctBy(p => p.BicepIdentifier);
        var distinctInfrastructureParametersLookup = distinctInfrastructureParameters.ToDictionary(p => p.BicepIdentifier);

        foreach (var aspireParameter in this.Parameters)
        {
            if (distinctInfrastructureParametersLookup.ContainsKey(aspireParameter.Key))
            {
                continue;
            }

            var isSecure = aspireParameter.Value is IResource r && r.TryGetParameter(out var p) && p.Secret || aspireParameter.Value is BicepSecretOutputReference;
            var parameter = new ProvisioningParameter(aspireParameter.Key, typeof(string)) { IsSecure = isSecure };
            infrastructure.Add(parameter);
        }

        // Add any "known" parameters the infrastructure is using to our Parameters
        // (except for 'location' because that is always inferred and shouldn't be in the manifest)
        foreach (var infrastructureParameter in distinctInfrastructureParameters)
        {
            if (KnownParameters.IsKnownParameterName(infrastructureParameter.BicepIdentifier) && infrastructureParameter.BicepIdentifier != KnownParameters.Location)
            {
                Parameters.TryAdd(infrastructureParameter.BicepIdentifier, null);
            }
        }
    }
}
