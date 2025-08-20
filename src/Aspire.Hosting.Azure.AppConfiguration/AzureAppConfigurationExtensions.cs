// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppConfiguration;
using Azure.Provisioning;
using Azure.Provisioning.AppConfiguration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AppConfiguration resources to the application model.
/// </summary>
public static class AzureAppConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// By default references to the Azure App Configuration resource will be assigned the following roles:
    /// 
    /// - <see cref="AppConfigurationBuiltInRole.AppConfigurationDataOwner"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureAppConfigurationResource}, AppConfigurationBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var store = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = AppConfigurationStore.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new AppConfigurationStore(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    SkuName = "standard",
                    DisableLocalAuth = true,
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            infrastructure.Add(new ProvisioningOutput("appConfigEndpoint", typeof(string)) { Value = store.Endpoint });

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = store.Name });
        };

        var resource = new AzureAppConfigurationResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(AppConfigurationBuiltInRole.GetBuiltInRoleName,
                AppConfigurationBuiltInRole.AppConfigurationDataOwner);
    }

    /// <summary>
    /// Configures an Azure App Configuration resource to be emulated. This resource requires an <see cref="AzureAppConfigurationResource"/> to be added to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="AppConfigurationEmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="AppConfigurationEmulatorContainerImageTags.Registry"/>/<inheritdoc cref="AppConfigurationEmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure App Configuration resource builder.</param>
    /// <param name="configureEmulator">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> RunAsEmulator(this IResourceBuilder<AzureAppConfigurationResource> builder, Action<IResourceBuilder<AzureAppConfigurationEmulatorResource>>? configureEmulator = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithHttpEndpoint(name: "emulator", targetPort: 8483)
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = AppConfigurationEmulatorContainerImageTags.Registry,
                Image = AppConfigurationEmulatorContainerImageTags.Image,
                Tag = AppConfigurationEmulatorContainerImageTags.Tag
            });

        var surrogate = new AzureAppConfigurationEmulatorResource(builder.Resource);
        var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
        surrogateBuilder.WithAnonymousAccess(role: "Owner"); // enable anonymous access by default

        if (configureEmulator != null)
        {
            configureEmulator(surrogateBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the storage of an Azure App Configuration emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureAppConfigurationEmulatorResource"/>.</param>
    /// <param name="path">Relative path to the AppHost where emulator storage is persisted between runs. Defaults to the path '.aace'</param>
    /// <returns>A builder for the <see cref="AzureAppConfigurationEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationEmulatorResource> WithDataBindMount(this IResourceBuilder<AzureAppConfigurationEmulatorResource> builder, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithBindMount(path ?? $".aace/{builder.Resource.Name}", "/app/.aace", isReadOnly: false);
    }

    /// <summary>
    /// Adds a named volume for the data folder to an Azure App Configuration emulator resource.
    /// </summary>
    /// <param name="builder">The builder for the <see cref="AzureAppConfigurationEmulatorResource"/>.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>A builder for the <see cref="AzureAppConfigurationEmulatorResource"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationEmulatorResource> WithDataVolume(this IResourceBuilder<AzureAppConfigurationEmulatorResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/app", isReadOnly: false);
    }

    /// <summary>
    /// Configures the host port for the Azure App Configuration emulator is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">Builder for the Azure App Configuration emulator container</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationEmulatorResource> WithHostPort(this IResourceBuilder<AzureAppConfigurationEmulatorResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure App Configuration resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure App Configuration resource.</param>
    /// <param name="roles">The built-in App Configuration roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the AppConfigurationDataReader role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var appStore = builder.AddAzureAppConfiguration("appStore");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(appStore, AppConfigurationBuiltInRole.AppConfigurationDataReader)
    ///   .WithReference(appStore);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureAppConfigurationResource> target,
        params AppConfigurationBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, AppConfigurationBuiltInRole.GetBuiltInRoleName, roles);
    }

    /// <summary>
    /// Configures anonymous authentication for the Azure App Configuration emulator resource.
    /// </summary>
    /// <param name="builder">The resource builder for the Azure App Configuration emulator.</param>
    /// <param name="role">The role to assign to the anonymous user. Defaults to "Owner".</param>
    /// <returns>The updated resource builder for further configuration.</returns>
    internal static IResourceBuilder<AzureAppConfigurationEmulatorResource> WithAnonymousAccess(this IResourceBuilder<AzureAppConfigurationEmulatorResource> builder, string role = "Owner")
    {
        builder.Resource.ConfigureAnonymousAuthentication(true, role);
        builder.WithEnvironment("Tenant:AnonymousAuthEnabled", "true");
        builder.WithEnvironment("Authentication:Anonymous:AnonymousUserRole", role);
        return builder;
    }
}
