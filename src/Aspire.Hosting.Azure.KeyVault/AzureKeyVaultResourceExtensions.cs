// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.KeyVault;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Key Vault resources to the application model.
/// </summary>
public static class AzureKeyVaultResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureKeyVault(name, null);
#pragma warning restore ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.KeyVaults.KeyVault"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("ASPIRE0001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name, Action<AzureKeyVaultDefinition>? configureResource)
    {
        return builder.AddAzureKeyVault<DefaultAzureKeyVaultAzureKeyVaultDefinition>(name, configureResource);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDefinition"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="configureResource"></param>
    /// <returns></returns>
    [Experimental("ASPIRE0001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault<TDefinition>(this IDistributedApplicationBuilder builder, string name, Action<TDefinition>? configureResource)
        where TDefinition : AzureKeyVaultDefinition
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var resource = (AzureKeyVaultResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition), construct, resourceBuilder)!;

            configureResource?.Invoke(definition);
        };
        var resource = new AzureKeyVaultResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
