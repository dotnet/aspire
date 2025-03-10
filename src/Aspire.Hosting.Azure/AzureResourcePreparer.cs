// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Prepares Azure resources for provisioning and publish.
///
/// This includes preparing role assignment annotations for Azure resources.
/// </summary>
internal sealed class AzureResourcePreparer(
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext
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
        if (options.UseDefaultRoleAssignments)
        {
            // If the app is using UseDefaultRoleAssignments, then we need to ensure that
            // there are no role assignment annotations in the app model because they won't be honored otherwise.
            EnsureNoRoleAssignmentAnnotations(appModel);
        }

        await BuildRoleAssignmentAnnotations(appModel, options.UseDefaultRoleAssignments, cancellationToken).ConfigureAwait(false);

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

    private static void EnsureNoRoleAssignmentAnnotations(DistributedApplicationModel appModel)
    {
        foreach (var resource in appModel.Resources)
        {
            if (resource.HasAnnotationOfType<RoleAssignmentAnnotation>())
            {
                throw new InvalidOperationException("The application model does not support role assignments. Ensure you are using a publisher that supports role assignments, for example AddAzureContainerAppsInfrastructure.");
            }
        }
    }

    private async Task BuildRoleAssignmentAnnotations(DistributedApplicationModel appModel, bool useDefaultRoleAssignments, CancellationToken cancellationToken)
    {
        if (useDefaultRoleAssignments)
        {
            // for useDefaultRoleAssignments, just copy all the default role assignments to applied role assignments
            foreach (var resource in appModel.Resources)
            {
                if (resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultRoleAssignments))
                {
                    AppendAppliedRoleAssignmentsAnnotation(resource, defaultRoleAssignments.Roles);
                }
            }
        }
        else
        {
            // when not using default role assignments, walk the resource graph and
            // - if in RunMode
            //   - If a compute resource has RoleAssignmentAnnotations, add them to AppliedRoleAssignmentsAnnotation on the referenced Azure resource
            //   - if the resource doesn't, copy the DefaultRoleAssignments to AppliedRoleAssignmentsAnnotation
            //
            // - if in PublishMode
            //   - If a compute resource has RoleAssignmentAnnotations, skip - the publish infrastructure will handle them
            //   - if the resource doesn't, copy the DefaultRoleAssignments to RoleAssignmentAnnotations so the publish infrastucture will apply the defaults
            foreach (var resource in appModel.Resources)
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
                            AppendAppliedRoleAssignmentsAnnotation(azureReference, roleAssignments.SelectMany(a => a.Roles));
                        }
                        // in PublishMode, this is a no-op since the publish infrastructure will handle the role assignments
                    }
                    else if (azureReference.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaults))
                    {
                        if (executionContext.IsRunMode)
                        {
                            // in RunMode, we copy the default role assignments to the Azure reference
                            AppendAppliedRoleAssignmentsAnnotation(azureReference, defaults.Roles);
                        }
                        else
                        {
                            // in PublishMode, we copy the default role assignments to the compute resource
                            resource.Annotations.Add(new RoleAssignmentAnnotation(azureReference, defaults.Roles));
                        }
                    }
                }
            }
        }
    }

    private async Task<HashSet<IAzureResource>> GetAzureReferences(IResource resource, CancellationToken cancellationToken)
    {
        if (resource.TryGetEnvironmentVariables(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, cancellationToken: cancellationToken);

            foreach (var c in environmentCallbacks)
            {
                await c.Callback(context).ConfigureAwait(false);
            }

            HashSet<IAzureResource> azureReferences = [];
            foreach (var kv in context.EnvironmentVariables)
            {
                ProcessAzureReferences(azureReferences, kv.Value);
            }

            return azureReferences;
        }

        return [];
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

    private static void AppendAppliedRoleAssignmentsAnnotation(IResource resource, IEnumerable<RoleDefinition> newRoles)
    {
        if (resource.TryGetLastAnnotation<AppliedRoleAssignmentsAnnotation>(out var appliedRoleAssignments))
        {
            appliedRoleAssignments.Roles.UnionWith(newRoles);
        }
        else
        {
            resource.Annotations.Add(new AppliedRoleAssignmentsAnnotation([..newRoles]));
        }
    }
}
