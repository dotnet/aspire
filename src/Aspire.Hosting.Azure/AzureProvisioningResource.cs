// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Aspire resource that supports use of Azure Provisioning APIs to create Azure resources.
/// </summary>
/// <param name="name">The name of the resource in the Aspire application model.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
public class AzureProvisioningResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureBicepResource(name, templateFile: $"{name}.module.bicep")
{
    /// <summary>
    /// Callback for configuring the Azure resources.
    /// </summary>
    public Action<AzureResourceInfrastructure> ConfigureInfrastructure { get; internal set; } = configureInfrastructure ?? throw new ArgumentNullException(nameof(configureInfrastructure));

    /// <summary>
    /// Gets or sets the <see cref="global::Azure.Provisioning.ProvisioningBuildOptions"/> which contains common settings and
    /// functionality for building Azure resources.
    /// </summary>
    public ProvisioningBuildOptions? ProvisioningBuildOptions { get; set; }

    /// <summary>
    /// Adds a new <see cref="ProvisionableResource"/> into <paramref name="infra"/>. The new resource
    /// represents a reference to the current <see cref="AzureProvisioningResource"/> via https://learn.microsoft.com/azure/azure-resource-manager/bicep/existing-resource.
    /// </summary>
    /// <param name="infra">The <see cref="AzureResourceInfrastructure"/> to add the existing resource into.</param>
    /// <returns>A new <see cref="ProvisionableResource"/>, typically using the FromExisting method on the derived <see cref="ProvisionableResource"/> class.</returns>
    public virtual ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra) => throw new NotImplementedException();

    /// <summary>
    /// Adds role assignments to this Azure resource.
    /// </summary>
    /// <param name="roleAssignmentContext">The context containing information about the role assignments and what principal to use.</param>
    public virtual void AddRoleAssignments(IAddRoleAssignmentsContext roleAssignmentContext)
    {
        var infra = roleAssignmentContext.Infrastructure;
        var prefix = this.GetBicepIdentifier();
        var existingResource = AddAsExistingResource(infra);

        var principalType = roleAssignmentContext.PrincipalType;
        var principalId = roleAssignmentContext.PrincipalId;

        foreach (var role in roleAssignmentContext.Roles)
        {
            infra.Add(CreateRoleAssignment(prefix, existingResource, role.Id, role.Name, principalType, principalId));
        }
    }

    private static RoleAssignment CreateRoleAssignment(string prefix, ProvisionableResource scope, string roleId, string roleName, BicepValue<RoleManagementPrincipalType> principalType, BicepValue<Guid> principalId)
    {
        var raName = Infrastructure.NormalizeBicepIdentifier($"{prefix}_{roleName}");
        var id = new MemberExpression(new IdentifierExpression(scope.BicepIdentifier), "id");

        return new RoleAssignment(raName)
        {
            Name = BicepFunction.CreateGuid(id, principalId, BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", roleId)),
            Scope = new IdentifierExpression(scope.BicepIdentifier),
            PrincipalType = principalType,
            RoleDefinitionId = BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", roleId),
            PrincipalId = principalId
        };
    }

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

    /// <summary>
    /// Encapsulates the logic for creating an existing or new <see cref="ProvisionableResource"/>
    /// based on whether or not the <see cref="ExistingAzureResourceAnnotation" /> exists on the resource.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ProvisionableResource"/> to produce.</typeparam>
    /// <param name="infrastructure">The <see cref="AzureResourceInfrastructure"/> that will contain the <see cref="ProvisionableResource"/>.</param>
    /// <param name="createExisting">A callback to create the existing resource.</param>
    /// <param name="createNew">A callback to create the new resource.</param>
    /// <returns>The provisioned resource.</returns>
    public static T CreateExistingOrNewProvisionableResource<T>(AzureResourceInfrastructure infrastructure, Func<string, BicepValue<string>, T> createExisting, Func<AzureResourceInfrastructure, T> createNew)
        where T : ProvisionableResource
    {
        ArgumentNullException.ThrowIfNull(infrastructure);
        ArgumentNullException.ThrowIfNull(createExisting);
        ArgumentNullException.ThrowIfNull(createNew);

        T provisionedResource;
        if (infrastructure.AspireResource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAnnotation))
        {
            var existingResourceName = existingAnnotation.Name is ParameterResource nameParameter
                ? nameParameter.AsProvisioningParameter(infrastructure)
                : new BicepValue<string>((string)existingAnnotation.Name);
            provisionedResource = createExisting(infrastructure.AspireResource.GetBicepIdentifier(), existingResourceName);
            if (existingAnnotation.ResourceGroup is not null)
            {
                infrastructure.AspireResource.Scope = new(existingAnnotation.ResourceGroup);
            }
        }
        else
        {
            provisionedResource = createNew(infrastructure);
        }
        infrastructure.Add(provisionedResource);
        return provisionedResource;
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

#pragma warning disable CS0618 // Type or member is obsolete
            var isSecure = aspireParameter.Value is ParameterResource { Secret: true } || aspireParameter.Value is BicepSecretOutputReference;
#pragma warning restore CS0618 // Type or member is obsolete
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
