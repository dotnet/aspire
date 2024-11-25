// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.SignalR;
using Azure.Provisioning;
using Azure.Provisioning.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SignalR resources to the application model.
/// </summary>
public static class AzureSignalRExtensions
{
    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var service = new SignalRService(infrastructure.AspireResource.GetBicepIdentifier())
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
                        Value = "Default"
                    }
                ],
                CorsAllowedOrigins = ["*"],
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(service);

            infrastructure.Add(new ProvisioningOutput("hostName", typeof(string)) { Value = service.HostName });

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(service.CreateRoleAssignment(SignalRBuiltInRole.SignalRAppServer, principalTypeParameter, principalIdParameter));
        };

        var resource = new AzureSignalRResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure SignalR resource to be emulated. This resource requires an <see cref="AzureSignalRResource"/> to be added to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="SignalREmulatorContainerImageTags.Tag"/> tag of the <inheritdoc cref="SignalREmulatorContainerImageTags.Registry"/>/<inheritdoc cref="SignalREmulatorContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> RunAsEmulator(this IResourceBuilder<AzureSignalRResource> builder, Action<IResourceBuilder<AzureSignalREmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        string? connectionString = null;
        builder
            .WithEndpoint(name: "emulator", targetPort: 8888, scheme: "http")
            .WithAnnotation(new ContainerImageAnnotation
            {
                Registry = SignalREmulatorContainerImageTags.Registry,
                Image = SignalREmulatorContainerImageTags.Image,
                Tag = SignalREmulatorContainerImageTags.Tag
            });

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(builder.Resource, async (@event, ct) =>
        {
            connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false)
                        ?? throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{builder.Resource.Name}' resource but the connection string was null.");
        });
        var healthCheckKey = $"{builder.Resource.Name}_check";
        var healthCheckRegistration = new HealthCheckRegistration(
            healthCheckKey,
            sp => {
                // Use SignalR Management SDK to init a client for health test
                var client = new ServiceManagerBuilder()
                .WithOptions(option => {
                    option.ConnectionString = connectionString ?? throw new InvalidOperationException("Connection string is unavailable");
                    option.ServiceTransportType = ServiceTransportType.Transient;
                })
                .BuildServiceManager();
                return new AzureSignalRHealthCheck(client);
            },
            failureStatus: default,
            tags: default
        );
        builder.ApplicationBuilder.Services.AddHealthChecks().Add(healthCheckRegistration);
        if (configureContainer != null)
        {
            var surrogate = new AzureSignalREmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }
        return builder.WithHealthCheck(healthCheckKey);
    }
}
