// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
/// <param name="name">The name of the construct in the Aspire application model.</param>
/// <param name="configureInfrastructure">Callback to populate the construct with Azure resources.</param>
public class AzureProvisioningResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// Callback for configuring construct.
    /// </summary>
    public Action<AzureResourceInfrastructure> ConfigureInfrastructure { get; internal set; } = configureInfrastructure;

    /// <summary>
    /// Gets or sets the <see cref="Azure.Provisioning.ProvisioningContext"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningContext? ProvisioningContext { get; set; }

    /// <inheritdoc/>
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var resourceModuleConstruct = new AzureResourceInfrastructure(this, Name);

        ConfigureInfrastructure(resourceModuleConstruct);

        // WARNING: GetParameters currently returns more than one instance of the same
        //          parameter. Its the only API that gives us what we need (a list of
        //          parameters. Here we find all the distinct parameters by name and
        //          put them into a dictionary for quick lookup so we don't need to scan
        //          through the parameter enumerable each time.
        var constructParameters = resourceModuleConstruct.GetParameters();
        var distinctConstructParameters = constructParameters.DistinctBy(p => p.IdentifierName);
        var distinctConstructParametersLookup = distinctConstructParameters.ToDictionary(p => p.IdentifierName);

        foreach (var aspireParameter in this.Parameters)
        {
            if (distinctConstructParametersLookup.ContainsKey(aspireParameter.Key))
            {
                continue;
            }

            var isSecure = aspireParameter.Value is ParameterResource { Secret: true } || aspireParameter.Value is BicepSecretOutputReference;
            var constructParameter = new ProvisioningParameter(aspireParameter.Key, typeof(string)) { IsSecure = isSecure };
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
