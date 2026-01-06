// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.AIFoundry;

/// <summary>
/// An Aspire wrapper around an Azure.Provisioning.ProvisionableResource.
/// </summary>
public abstract class AzureResourceManagerAspireResource<T>(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure)
    where T : ProvisionableResource
{
    /// <summary>
    /// Get the underlying provisionable resource of type T for the given AzureProvisioningResource.
    /// </summary>
    public static T? GetProvisionableResource(AzureResourceInfrastructure infra, string bicepIdentifier)
    {
        return infra.GetProvisionableResources()
            .OfType<T>()
            .SingleOrDefault(r => r.BicepIdentifier == bicepIdentifier);
    }

    /// <summary>
    /// Provide the (usually unique) name of the resource
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <inheritdoc/>
    public override T AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var existing = GetProvisionableResource(infra, bicepIdentifier);
        if (existing is not null)
        {
            return existing;
        }
        var created = FromExisting(bicepIdentifier);
        // If Aspire resource`this` has an ExistingResourceAnnotation, update Provisionable resource `created`
        // to match it (name and resource group). Otherwise continue using as is.
        if (!TryApplyExistingResourceAnnotation(this, infra, created))
        {
            SetName(created, this.NameOutputReference.AsProvisioningParameter(infra));
        }
        infra.Add(created);
        return created;
    }

    /// <summary>
    /// Sets the name of the provisionable resource.
    ///
    /// This is needed because not all provisionable resources have a name property with a setter.
    /// </summary>
    public abstract void SetName(T provisionableResource, BicepValue<string> name);

    /// <summary>
    /// Sets the name of the provisionable resource.
    ///
    /// This is needed because not all provisionable resources have a name property with a setter.
    /// </summary>
    public abstract T FromExisting(string bicepIdentifier);

    // /// <summary>
    // /// Gets a Provisionable resource from existing resource.
    // /// </summary>
    // /// <param name="bicepIdentifier"></param>
    // /// <param name="resourceName"></param>
    // /// <returns></returns>
    // /// <exception cref="InvalidOperationException"></exception>
    // public static T GetProvisionableFromExisting(string bicepIdentifier, BicepValue<string> resourceName)
    // {
    //     var fromExisting = typeof(T).GetMethod("FromExisting", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not have a public static FromExisting method.");
    //     var result = (T)(fromExisting.Invoke(null, [bicepIdentifier]) ?? throw new InvalidOperationException($"FromExisting method on type '{typeof(T).FullName}' returned null."));
    //     return result;
    // }
}

/// <summary>
/// An AzureResourceManagerResource that also is IResourceWithParent.
/// </summary>
public abstract class AzureResourceManagerAspireResourceWithParent<T, P>(string name, Action<AzureResourceInfrastructure> configureInfrastructure, P parent) :
    AzureResourceManagerAspireResource<T>(name, configureInfrastructure), IResourceWithParent<P>
    where T : ProvisionableResource
    where P : AzureProvisioningResource
{
    /// <summary>
    /// Gets the parent resource.
    /// </summary>
    public P Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));
}

/// <summary>
/// Extension methods for <see cref="AzureResourceManagerAspireResource{T}"/>.
/// </summary>
public static class AzureResourceManagerResourceExtensions
{
    /// <summary>
    /// Configure the underlying Azure ProvisioningResource for situations
    /// where additional customization is needed.
    /// </summary>
    /// <typeparam name="A">The type of the Aspire resource.</typeparam>
    /// <typeparam name="P">The type of the underlying Provisionable resource</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">An callback to configure the Provisionable resource.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<A> WithConfiguration<A, P>(this IResourceBuilder<A> builder, Action<P> configure)
        where A : AzureResourceManagerAspireResource<P>
        where P : ProvisionableResource
    {
        builder.ConfigureInfrastructure(infra =>
        {
            var r = AzureResourceManagerAspireResource<P>.GetProvisionableResource(infra, builder.Resource.GetBicepIdentifier()) ?? throw new InvalidOperationException($"Provisionable resource for Aspire resource '{builder.Resource.Name}' not found in infrastructure.");
            configure(r);
        });
        return builder;
    }
}
