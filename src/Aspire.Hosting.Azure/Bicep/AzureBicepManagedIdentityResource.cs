// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A Bicep resource that represents a Azure User-Assigned Managed Identity.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepManagedIdentityResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.managedIdentity.bicep")
{
    /// <summary>
    /// The principalId output reference.
    /// </summary>
    public BicepOutputReference PrincipalId => new("principalId", this);

    /// <summary>
    /// The clientId output reference.
    /// </summary>
    public BicepOutputReference ClientId => new("clientId", this);
}

/// <summary>
/// Represents an Azure resource that supports specifying a managed identity.
/// </summary>
public interface IAzureResourceWithManagedIdentity : IAzureResource { }

/// <summary>
/// Extension methods for <see cref="AzureBicepManagedIdentityResource"/>.
/// </summary>
public static class AzureBicepManagedIdentityExtensions
{
    /// <summary>
    /// Adds a Bicep resource that represents a Azure User-Assigned Managed Identity.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureBicepManagedIdentityResource> AddBicepAzureManagedIdentity(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepManagedIdentityResource(name);

        return builder.AddResource(resource)
                      .WithParameter("managedIdentityName", resource.CreateBicepResourceName())
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Sets the identity of the resource to the specified managed identity. This will set the identity and identityType parameters for the bicep resource.
    /// </summary>
    /// <typeparam name="T">The <see cref="AzureBicepResource"/>.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="identity">The managed identity resource builder.</param>
    /// <returns>A configured resource builder.</returns>
    public static IResourceBuilder<T> WithIdentity<T>(this IResourceBuilder<T> builder, IResourceBuilder<AzureBicepManagedIdentityResource> identity)
        where T : AzureBicepResource, IAzureResourceWithManagedIdentity
    {
        return builder.WithParameter("identity", identity.Resource.PrincipalId)
                      .WithParameter("identityType", "ServicePrincipal");
    }

    /// <summary>
    /// Assigns the user-assigned managed identity to the project resource. This will set the AZURE_CLIENT_ID environment variable.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="identityBuilder">The managed identity resource builder.</param>
    /// <returns> A configured resource builder.</returns>
    public static IResourceBuilder<T> WithReference<T>(this IResourceBuilder<T> builder, IResourceBuilder<AzureBicepManagedIdentityResource> identityBuilder)
        where T : IResourceWithEnvironment
    {
        var identity = identityBuilder.Resource;

        return builder.WithEnvironment(context =>
        {
            if (context.ExecutionContext.Operation == DistributedApplicationOperation.Publish)
            {
                context.EnvironmentVariables["AZURE_CLIENT_ID"] = identity.ClientId.ValueExpression;
                return;
            }

            context.EnvironmentVariables["AZURE_CLIENT_ID"] = identity.ClientId.Value!;
        });
    }
}
