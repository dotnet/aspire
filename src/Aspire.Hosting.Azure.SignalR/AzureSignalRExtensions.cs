// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.SignalR;
using Azure.Provisioning;
using Azure.Provisioning.SignalR;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SignalR resources to the application model.
/// </summary>
public static class AzureSignalRExtensions
{
    private const string EmulatorEndpointName = "emulator";

    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// By default references to the Azure SignalR resource will be assigned the following roles:
    /// 
    /// - <see cref="SignalRBuiltInRole.SignalRAppServer"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureSignalRResource}, SignalRBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceName] string name)
        => AddAzureSignalR(builder, name, AzureSignalRServiceMode.Default);

    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="serviceMode">The service mode of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// By default references to the Azure SignalR resource will be assigned the following roles:
    /// 
    /// - <see cref="SignalRBuiltInRole.SignalRAppServer"/>
    ///
    /// Using <see cref="AzureSignalRServiceMode.Serverless"/> additionally adds:
    ///
    /// - <see cref="SignalRBuiltInRole.SignalRRestApiOwner"/>
    /// 
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureSignalRResource}, SignalRBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceName] string name, AzureSignalRServiceMode serviceMode)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var service = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure, (identifier, name) =>
            {
                var resource = SignalRService.FromExisting(identifier);
                resource.Name = name;
                return resource;
            },

            (infrastructure) => new SignalRService(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Kind = SignalRServiceKind.SignalR,
                Sku = new SignalRResourceSku()
                {
                    Name = "Free_F1",
                    Capacity = 1
                },
                Features =
                [
                    new SignalRFeature()
                    {
                        Flag = SignalRFeatureFlag.ServiceMode,
                        Value = serviceMode.ToString()
                    }
                ],
                CorsAllowedOrigins = ["*"],
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            });

            infrastructure.Add(new ProvisioningOutput("hostName", typeof(string)) { Value = service.HostName });

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = service.Name });
        };

        List<SignalRBuiltInRole> defaultRoles = [SignalRBuiltInRole.SignalRAppServer];
        if (serviceMode == AzureSignalRServiceMode.Serverless)
        {
            defaultRoles.Add(SignalRBuiltInRole.SignalRRestApiOwner);
        }

        var resource = new AzureSignalRResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(SignalRBuiltInRole.GetBuiltInRoleName, defaultRoles.ToArray());
    }

    /// <summary>
    /// Configures an Azure SignalR resource to be emulated. This resource requires an <see cref="AzureSignalRResource"/> to be added to the application model. Please note that the resource will be emulated in <b>Serverless mode</b>.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="SignalREmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="SignalREmulatorContainerImageTags.Registry"/>/<inheritdoc cref="SignalREmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure SignalR resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> RunAsEmulator(this IResourceBuilder<AzureSignalRResource> builder, Action<IResourceBuilder<AzureSignalREmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder
            .WithEndpoint(name: EmulatorEndpointName, targetPort: 8888, scheme: "http")
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = SignalREmulatorContainerImageTags.Registry,
                Image = SignalREmulatorContainerImageTags.Image,
                Tag = SignalREmulatorContainerImageTags.Tag
            });
        if (configureContainer != null)
        {
            var surrogate = new AzureSignalREmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }
        return builder.WithHttpHealthCheck(endpointName: EmulatorEndpointName, path: "/api/health");
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure SignalR resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure SignalR resource.</param>
    /// <param name="roles">The built-in SignalR roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the SignalRContributor role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var signalr = builder.AddAzureSignalR("signalr");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(signalr, SignalRBuiltInRole.SignalRContributor)
    ///   .WithReference(signalr);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureSignalRResource> target,
        params SignalRBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, SignalRBuiltInRole.GetBuiltInRoleName, roles);
    }
}
