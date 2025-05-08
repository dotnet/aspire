// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Azure.Provisioning;
using Azure.Provisioning.Authorization;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Prepares Azure resources for provisioning and publish.
///
/// This includes preparing role assignment annotations for Azure resources.
/// </summary>
internal sealed class AzureResourcePreparer(
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService
    ) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var azureResources = GetAzureResourcesFromAppModel(appModel);
        if (azureResources.Count == 0)
        {
            return;
        }

        var options = provisioningOptions.Value;
        if (!EnvironmentSupportsTargetedRoleAssignments(options))
        {
            // If the app infrastructure does not support targeted role assignments, then we need to ensure that
            // there are no role assignment annotations in the app model because they won't be honored otherwise.
            EnsureNoRoleAssignmentAnnotations(appModel);
        }

        await BuildRoleAssignmentAnnotations(appModel, azureResources, options, cancellationToken).ConfigureAwait(false);

        // set the ProvisioningBuildOptions on the resource, if necessary
        foreach (var r in azureResources)
        {
            if (r.AzureResource is AzureProvisioningResource provisioningResource)
            {
                provisioningResource.ProvisioningBuildOptions = options.ProvisioningBuildOptions;
            }
        }
    }

    internal static List<(IResource Resource, IAzureResource AzureResource)> GetAzureResourcesFromAppModel(DistributedApplicationModel appModel)
    {
        // Some resources do not derive from IAzureResource but can be handled
        // by the Azure provisioner because they have the AzureBicepResourceAnnotation
        // which holds a reference to the surrogate AzureBicepResource which implements
        // IAzureResource and can be used by the Azure Bicep Provisioner.

        var azureResources = new List<(IResource, IAzureResource)>();
        foreach (var resource in appModel.Resources)
        {
            if (resource.IsContainer())
            {
                continue;
            }
            else if (resource is IAzureResource azureResource)
            {
                // If we are dealing with an Azure resource then we just return it.
                azureResources.Add((resource, azureResource));
            }
            else if (resource.Annotations.OfType<AzureBicepResourceAnnotation>().SingleOrDefault() is { } annotation)
            {
                // If we aren't an Azure resource and there is no surrogate, return null for
                // the Azure resource in the tuple (we'll filter it out later.
                azureResources.Add((resource, annotation.Resource));
            }
        }

        return azureResources;
    }

    private bool EnvironmentSupportsTargetedRoleAssignments(AzureProvisioningOptions options)
    {
        // run mode always supports targeted role assignments
        // publish mode only supports targeted role assignments if the environment supports it
        return executionContext.IsRunMode || options.SupportsTargetedRoleAssignments;
    }

    private static void EnsureNoRoleAssignmentAnnotations(DistributedApplicationModel appModel)
    {
        foreach (var resource in appModel.Resources)
        {
            if (resource.HasAnnotationOfType<RoleAssignmentAnnotation>())
            {
                throw new InvalidOperationException("The application model does not support role assignments. Ensure you are using an environment that supports role assignments, for example AddAzureContainerAppEnvironment.");
            }
        }
    }

    private async Task BuildRoleAssignmentAnnotations(DistributedApplicationModel appModel, List<(IResource Resource, IAzureResource AzureResource)> azureResources, AzureProvisioningOptions options, CancellationToken cancellationToken)
    {
        var globalRoleAssignments = new Dictionary<AzureProvisioningResource, HashSet<RoleDefinition>>();

        if (!EnvironmentSupportsTargetedRoleAssignments(options))
        {
            // when the app infrastructure doesn't support targeted role assignments, just copy all the default role assignments to applied role assignments
            foreach (var resource in azureResources.Select(r => r.AzureResource).OfType<AzureProvisioningResource>())
            {
                if (resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultRoleAssignments))
                {
                    AppendGlobalRoleAssignments(globalRoleAssignments, resource, defaultRoleAssignments.Roles);
                }
            }
        }
        else
        {
            // when the app infrastructure supports targeted role assignments, walk the resource graph and
            // - if in RunMode
            //   - if a compute resource has RoleAssignmentAnnotations, add them to globalRoleAssignments on the referenced Azure resource
            //   - if the resource doesn't, copy the DefaultRoleAssignments to globalRoleAssignments
            //
            // - if in PublishMode
            //   - if a compute resource has RoleAssignmentAnnotations, use them
            //   - if the resource doesn't, copy the DefaultRoleAssignments to RoleAssignmentAnnotations to apply the defaults
            var resourceSnapshot = appModel.Resources.ToArray(); // avoid modifying the collection while iterating
            foreach (var resource in resourceSnapshot)
            {
                if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
                {
                    continue;
                }

                if (!resource.IsContainer() && resource is not ProjectResource)
                {
                    continue;
                }

                var azureReferences = await GetAzureReferences(resource, cancellationToken).ConfigureAwait(false);

                var azureReferencesWithRoleAssignments =
                    (resource.TryGetAnnotationsOfType<RoleAssignmentAnnotation>(out var annotations)
                        ? annotations
                        : [])
                        .ToLookup(a => a.Target);
                foreach (var azureReference in azureReferences.OfType<AzureProvisioningResource>())
                {
                    var roleAssignments = azureReferencesWithRoleAssignments[azureReference];
                    if (roleAssignments.Any())
                    {
                        if (executionContext.IsRunMode)
                        {
                            // in RunMode, we need to add the role assignments to the resource
                            AppendGlobalRoleAssignments(globalRoleAssignments, azureReference, roleAssignments.SelectMany(a => a.Roles));
                        }
                        // in PublishMode, this is a no-op since GetAllRoleAssignments will handle the role assignments
                    }
                    else if (azureReference.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaults))
                    {
                        if (executionContext.IsRunMode)
                        {
                            // in RunMode, we copy the default role assignments to the Azure reference,
                            // even if the roles are empty, since empty roles are used by some resources - like databases
                            AppendGlobalRoleAssignments(globalRoleAssignments, azureReference, defaults.Roles);
                        }
                        else
                        {
                            // in PublishMode, we copy the default role assignments to the compute resource
                            resource.Annotations.Add(new RoleAssignmentAnnotation(azureReference, defaults.Roles));
                        }
                    }
                }

                // in PublishMode with SupportsTargetedRoleAssignments, we need to create the identity and role assignment resources
                // if the resource references any Azure resources, or has role assignments to Azure resources
                if (executionContext.IsPublishMode)
                {
                    var roleAssignments = GetAllRoleAssignments(resource);
                    if (roleAssignments.Count > 0)
                    {
                        var (identityResource, roleAssignmentResources) = CreateIdentityAndRoleAssignmentResources(options, resource, roleAssignments, executionContext);

                        // attach the identity resource to compute resource so it can be used by the compute environment
                        resource.Annotations.Add(new AppIdentityAnnotation(identityResource));

                        appModel.Resources.Add(identityResource);
                        foreach (var roleAssignmentResource in roleAssignmentResources)
                        {
                            appModel.Resources.Add(roleAssignmentResource);
                        }
                    }
                }
            }

            if (executionContext.IsRunMode)
            {
                // in RunMode, any Azure resources that are not referenced by a compute resource should have their default role assignments applied
                foreach (var azureResource in azureResources.Select(r => r.AzureResource).OfType<AzureProvisioningResource>())
                {
                    if (!globalRoleAssignments.TryGetValue(azureResource, out _) &&
                        azureResource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultRoleAssignments))
                    {
                        AppendGlobalRoleAssignments(globalRoleAssignments, azureResource, defaultRoleAssignments.Roles);
                    }
                }
            }
        }

        if (globalRoleAssignments.Count > 0)
        {
            await CreateGlobalRoleAssignments(appModel, globalRoleAssignments, options).ConfigureAwait(false);
        }
    }

    private static Dictionary<AzureProvisioningResource, IEnumerable<RoleDefinition>> GetAllRoleAssignments(IResource resource)
    {
        var result = new Dictionary<AzureProvisioningResource, IEnumerable<RoleDefinition>>();
        if (resource.TryGetAnnotationsOfType<RoleAssignmentAnnotation>(out var roleAssignments))
        {
            foreach (var g in roleAssignments.GroupBy(r => r.Target))
            {
                result[g.Key] = g.SelectMany(r => r.Roles);
            }
        }
        return result;
    }

    private static (AppIdentityResource IdentityResource, List<AzureBicepResource> RoleAssignmentResources) CreateIdentityAndRoleAssignmentResources(
        AzureProvisioningOptions provisioningOptions,
        IResource resource,
        Dictionary<AzureProvisioningResource, IEnumerable<RoleDefinition>> roleAssignments,
        DistributedApplicationExecutionContext executionContext)
    {
        var identityResource = new AppIdentityResource($"{resource.Name}-identity")
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
        };

        var roleAssignmentResources = CreateRoleAssignmentsResources(provisioningOptions, resource, roleAssignments, identityResource, executionContext);
        return (identityResource, roleAssignmentResources);
    }

    private static List<AzureBicepResource> CreateRoleAssignmentsResources(
        AzureProvisioningOptions provisioningOptions,
        IResource resource,
        Dictionary<AzureProvisioningResource, IEnumerable<RoleDefinition>> roleAssignments,
        AppIdentityResource appIdentityResource,
        DistributedApplicationExecutionContext executionContext)
    {
        var roleAssignmentResources = new List<AzureBicepResource>();
        foreach (var (targetResource, roles) in roleAssignments)
        {
            var roleAssignmentResource = new AzureProvisioningResource(
                $"{resource.Name}-roles-{targetResource.Name}",
                infra => AddRoleAssignmentsInfrastructure(infra, executionContext, targetResource, roles, appIdentityResource))
            {
                ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions,
            };

            // existing resource role assignments need to be scoped to the resource's resource group
            if (targetResource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAnnotation) &&
                existingAnnotation.ResourceGroup is not null)
            {
                roleAssignmentResource.Scope = new(existingAnnotation.ResourceGroup);
            }

            roleAssignmentResources.Add(roleAssignmentResource);
        }

        return roleAssignmentResources;
    }

    private static void AddRoleAssignmentsInfrastructure(
        AzureResourceInfrastructure infra,
        DistributedApplicationExecutionContext executionContext,
        AzureProvisioningResource azureResource,
        IEnumerable<RoleDefinition> roles,
        AppIdentityResource appIdentityResource)
    {
        
        var context = new AddRoleAssignmentsContext(
            infra,
            executionContext,
            roles,
            new(() => RoleManagementPrincipalType.ServicePrincipal),
            new(() => appIdentityResource.PrincipalId.AsProvisioningParameter(infra, parameterName: AzureBicepResource.KnownParameters.PrincipalId)),
            new(() => appIdentityResource.PrincipalName.AsProvisioningParameter(infra, parameterName: AzureBicepResource.KnownParameters.PrincipalName)));

        azureResource.AddRoleAssignments(context);
    }

    /// <summary>
    /// Context for adding role assignments to an Azure resource.
    /// </summary>
    private sealed class AddRoleAssignmentsContext(
        AzureResourceInfrastructure infrastructure,
        DistributedApplicationExecutionContext executionContext,
        IEnumerable<RoleDefinition> roles,
        Lazy<BicepValue<RoleManagementPrincipalType>> getPrincipalType,
        Lazy<BicepValue<Guid>> getPrincipalId,
        Lazy<BicepValue<string>> getPrincipalName) : IAddRoleAssignmentsContext
    {
        public AzureResourceInfrastructure Infrastructure { get; } = infrastructure;

        public IEnumerable<RoleDefinition> Roles { get; } = roles;

        public BicepValue<RoleManagementPrincipalType> PrincipalType => getPrincipalType.Value;

        public BicepValue<Guid> PrincipalId => getPrincipalId.Value;

        public BicepValue<string> PrincipalName => getPrincipalName.Value;

        public DistributedApplicationExecutionContext ExecutionContext => executionContext;
    }

    private async Task<HashSet<IAzureResource>> GetAzureReferences(IResource resource, CancellationToken cancellationToken)
    {
        HashSet<IAzureResource> azureReferences = [];

        if (resource.TryGetEnvironmentVariables(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, resource, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            foreach (var kv in context.EnvironmentVariables)
            {
                ProcessAzureReferences(azureReferences, kv.Value);
            }
        }

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var commandLineArgsCallbackAnnotations))
        {
            var context = new CommandLineArgsCallbackContext([], cancellationToken: cancellationToken);

            foreach (var c in commandLineArgsCallbackAnnotations)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            foreach (var arg in context.Args)
            {
                ProcessAzureReferences(azureReferences, arg);
            }
        }

        return azureReferences;
    }

    private static void ProcessAzureReferences(HashSet<IAzureResource> azureReferences, object value)
    {
        if (value is string or EndpointReference or ParameterResource or EndpointReferenceExpression or HostUrl)
        {
            return;
        }

        if (value is ConnectionStringReference cs)
        {
            if (cs.Resource is IAzureResource ar)
            {
                azureReferences.Add(ar);
            }

            ProcessAzureReferences(azureReferences, cs.Resource.ConnectionStringExpression);
            return;
        }

        if (value is IResourceWithConnectionString csrs)
        {
            if (csrs is IAzureResource ar)
            {
                azureReferences.Add(ar);
            }

            ProcessAzureReferences(azureReferences, csrs.ConnectionStringExpression);
            return;
        }

        if (value is BicepOutputReference output)
        {
            azureReferences.Add(output.Resource);
            return;
        }

        if (value is BicepSecretOutputReference secretOutputReference)
        {
            azureReferences.Add(secretOutputReference.Resource);
            return;
        }

        if (value is IAzureKeyVaultSecretReference keyVaultSecretReference)
        {
            azureReferences.Add(keyVaultSecretReference.Resource);
            return;
        }

        if (value is ReferenceExpression expr)
        {
            foreach (var vp in expr.ValueProviders)
            {
                ProcessAzureReferences(azureReferences, vp);
            }
            return;
        }

        throw new NotSupportedException("Unsupported value type " + value.GetType());
    }
    private static void AppendGlobalRoleAssignments(Dictionary<AzureProvisioningResource, HashSet<RoleDefinition>> globalRoleAssignments, AzureProvisioningResource azureResource, IEnumerable<RoleDefinition> newRoles)
    {
        if (!globalRoleAssignments.TryGetValue(azureResource, out var existingRoles))
        {
            existingRoles = new HashSet<RoleDefinition>();
            globalRoleAssignments[azureResource] = existingRoles;
        }

        existingRoles.UnionWith(newRoles);
    }

    private async Task CreateGlobalRoleAssignments(DistributedApplicationModel appModel, Dictionary<AzureProvisioningResource, HashSet<RoleDefinition>> globalRoleAssignments, AzureProvisioningOptions provisioningOptions)
    {
        foreach (var (azureResource, roles) in globalRoleAssignments)
        {
            var roleAssignmentResource = CreateGlobalRoleAssignmentsResource(provisioningOptions, azureResource, roles, executionContext);
            appModel.Resources.Add(roleAssignmentResource);

            azureResource.Annotations.Add(new RoleAssignmentResourceAnnotation(roleAssignmentResource));

            roleAssignmentResource.Annotations.Add(new ResourceRelationshipAnnotation(azureResource, KnownRelationshipTypes.Parent));
            await notificationService.PublishUpdateAsync(roleAssignmentResource, s => s with
            {
                Properties = s.Properties.SetResourceProperty(KnownProperties.Resource.ParentName, azureResource.Name)
            }).ConfigureAwait(false);
        }
    }

    private static AzureProvisioningResource CreateGlobalRoleAssignmentsResource(
        AzureProvisioningOptions provisioningOptions,
        AzureProvisioningResource targetResource,
        IEnumerable<RoleDefinition> roles,
        DistributedApplicationExecutionContext executionContext)
    {
        var roleAssignmentResource = new AzureProvisioningResource(
            $"{targetResource.Name}-roles",
            infra => AddGlobalRoleAssignmentsInfrastructure(infra, executionContext, targetResource, roles))
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions,
        };

        // existing resource role assignments need to be scoped to the resource's resource group
        if (targetResource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAnnotation) &&
            existingAnnotation.ResourceGroup is not null)
        {
            roleAssignmentResource.Scope = new(existingAnnotation.ResourceGroup);
        }

        return roleAssignmentResource;
    }

    private static void AddGlobalRoleAssignmentsInfrastructure(
        AzureResourceInfrastructure infra,
        DistributedApplicationExecutionContext executionContext,
        AzureProvisioningResource azureResource,
        IEnumerable<RoleDefinition> roles)
    {
        ProvisioningParameter CreatePrincipalParam(string name)
        {
            var param = new ProvisioningParameter(name, typeof(string));
            infra.Add(param);
            return param;
        }

        var context = new AddRoleAssignmentsContext(
            infra,
            executionContext,
            roles,
            new(() => CreatePrincipalParam(AzureBicepResource.KnownParameters.PrincipalType)),
            new(() => CreatePrincipalParam(AzureBicepResource.KnownParameters.PrincipalId)),
            new(() => CreatePrincipalParam(AzureBicepResource.KnownParameters.PrincipalName)));

        azureResource.AddRoleAssignments(context);
    }
}
