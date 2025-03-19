// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.WebPubSub;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Web PubSub resources to the application model.
/// </summary>
public static class AzureWebPubSubExtensions
{
    /// <summary>
    /// Adds an Azure Web PubSub resource to the application model.
    /// Change sku: WithParameter("sku", "Standard_S1")
    /// Change capacity: WithParameter("capacity", 2)
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// By default references to the Azure Web PubSub resource will be assigned the following roles:
    /// 
    /// - <see cref="WebPubSubBuiltInRole.WebPubSubServiceOwner"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureWebPubSubResource}, WebPubSubBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var service = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = WebPubSubService.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    // Supported values are Free_F1 Standard_S1 Premium_P1
                    var skuParameter = new ProvisioningParameter("sku", typeof(string))
                    {
                        Value = "Free_F1"
                    };
                    infrastructure.Add(skuParameter);

                    // Supported values are 1 2 5 10 20 50 100
                    var capacityParameter = new ProvisioningParameter("capacity", typeof(int))
                    {
                        Value = new BicepValue<int>(1)
                    };
                    infrastructure.Add(capacityParameter);

                    var service = new WebPubSubService(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        Sku = new BillingInfoSku()
                        {
                            Name = skuParameter,
                            Capacity = capacityParameter
                        },
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };
                    return service;
                }
            );

            infrastructure.Add(new ProvisioningOutput("endpoint", typeof(string)) { Value = BicepFunction.Interpolate($"https://{service.HostName}") });

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = service.Name });

            if (infrastructure.AspireResource.TryGetLastAnnotation<AppliedRoleAssignmentsAnnotation>(out var appliedRoleAssignments))
            {
                var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
                infrastructure.Add(principalTypeParameter);
                var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
                infrastructure.Add(principalIdParameter);

                foreach (var role in appliedRoleAssignments.Roles)
                {
                    infrastructure.Add(service.CreateRoleAssignment(new WebPubSubBuiltInRole(role.Id), principalTypeParameter, principalIdParameter));
                }
            }

            var resource = (AzureWebPubSubResource)infrastructure.AspireResource;
            foreach (var setting in resource.Hubs)
            {
                var hubName = setting.Key;

                var hubBuilder = setting.Value;
                var hubResource = hubBuilder;
                var hub = new WebPubSubHub(Infrastructure.NormalizeBicepIdentifier(hubResource.Name))
                {
                    Name = setting.Key,
                    Parent = service,
                    Properties = new WebPubSubHubProperties()
                };

                var hubProperties = hub.Properties;

                // invoke the configure from AddEventHandler
                for (var i = 0; i < hubResource.EventHandlers.Count; i++)
                {
                    var (url, userEvents, systemEvents, auth) = hubResource.EventHandlers[i];
                    var urlExpression = url;

                    BicepValue<string> urlParameter;
                    if (urlExpression.ManifestExpressions.Count == 0)
                    {
                        // when urlExpression is literal string, simply add
                        urlParameter = urlExpression.Format;
                    }
                    else
                    {
                        // otherwise add parameter to the construct
                        var parameter = new ProvisioningParameter($"{hubName}_url_{i}", typeof(string));
                        infrastructure.Add(parameter);
                        resource.Parameters[parameter.BicepIdentifier] = urlExpression;
                        urlParameter = parameter;
                    }

                    var eventHandler = GetWebPubSubEventHandler(urlParameter, userEvents, systemEvents, auth);

                    hubProperties.EventHandlers.Add(eventHandler);
                }

                // add to construct
                infrastructure.Add(hub);
            }
        };

        var resource = new AzureWebPubSubResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(WebPubSubBuiltInRole.GetBuiltInRoleName,
                WebPubSubBuiltInRole.WebPubSubServiceOwner);
    }

    /// <summary>
    /// Add hub settings
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="hubName">The hub name. Hub name is case-insensitive.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureWebPubSubHubResource> AddHub(this IResourceBuilder<AzureWebPubSubResource> builder, [ResourceName] string hubName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(hubName);

        AzureWebPubSubHubResource? hubResource;
        if (!builder.Resource.Hubs.TryGetValue(hubName, out hubResource))
        {
            hubResource = new AzureWebPubSubHubResource(hubName, builder.Resource);
            builder.Resource.Hubs[hubName] = hubResource;
        }
        var hubBuilder = builder.ApplicationBuilder.CreateResourceBuilder(hubResource);
        return hubBuilder;
    }

    /// <summary>
    /// Add event handler setting with expression
    /// </summary>
    /// <param name="builder">The builder for a Web PubSub hub.</param>
    /// <param name="urlTemplateExpression">The expression to evaluate the URL template configured for the event handler.</param>
    /// <param name="userEventPattern">The user event pattern for the event handler.</param>
    /// <param name="systemEvents">The system events for the event handler.</param>
    /// <param name="authSettings">The auth settings configured for the event handler.</param>
    /// <returns></returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static IResourceBuilder<AzureWebPubSubHubResource> AddEventHandler(
        this IResourceBuilder<AzureWebPubSubHubResource> builder,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        ReferenceExpression.ExpressionInterpolatedStringHandler urlTemplateExpression,
        string userEventPattern = "*",
        string[]? systemEvents = null,
        UpstreamAuthSettings? authSettings = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userEventPattern);

        var urlExpression = ReferenceExpression.Create(urlTemplateExpression);

        return AddEventHandler(builder, urlExpression, userEventPattern, systemEvents, authSettings);
    }

    /// <summary>
    /// Add event handler setting with expression
    /// </summary>
    /// <param name="builder">The builder for a Web PubSub hub.</param>
    /// <param name="urlExpression">The expression to evaluate the URL template configured for the event handler.</param>
    /// <param name="userEventPattern">The user event pattern for the event handler.</param>
    /// <param name="systemEvents">The system events for the event handler.</param>
    /// <param name="authSettings">The auth settings configured for the event handler.</param>
    /// <returns></returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static IResourceBuilder<AzureWebPubSubHubResource> AddEventHandler(
        this IResourceBuilder<AzureWebPubSubHubResource> builder,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        ReferenceExpression urlExpression,
        string userEventPattern = "*",
        string[]? systemEvents = null,
        UpstreamAuthSettings? authSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(urlExpression);
        ArgumentException.ThrowIfNullOrEmpty(userEventPattern);

        builder.Resource.EventHandlers.Add((urlExpression, userEventPattern, systemEvents, authSettings));
        return builder;
    }

    private static WebPubSubEventHandler GetWebPubSubEventHandler(BicepValue<string> urlValue, string userEventPattern, string[]? systemEvents, UpstreamAuthSettings? authSettings)
    {
        var handler = new WebPubSubEventHandler
        {
            UrlTemplate = urlValue,
            UserEventPattern = userEventPattern,
        };

        if (systemEvents != null)
        {
            handler.SystemEvents = [.. systemEvents];
        }

        if (authSettings != null)
        {
            handler.Auth = authSettings;
        }
        return handler;
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure Web PubSub resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure Web PubSub resource.</param>
    /// <param name="roles">The built-in Web PubSub roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <example>
    /// Assigns the WebPubSubServiceReader role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var webPubSub = builder.AddAzureWebPubSub("webPubSub");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(webPubSub, WebPubSubBuiltInRole.WebPubSubServiceReader)
    ///   .WithReference(webPubSub);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureWebPubSubResource> target,
        params WebPubSubBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, WebPubSubBuiltInRole.GetBuiltInRoleName, roles);
    }
}
