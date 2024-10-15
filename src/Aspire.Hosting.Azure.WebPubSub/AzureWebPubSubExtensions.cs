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
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            // Supported values are Free_F1 Standard_S1 Premium_P1
            var skuParameter = new ProvisioningParameter("sku", typeof(string))
            {
                Value = new StringLiteral("Free_F1")
            };
            construct.Add(skuParameter);

            // Supported values are 1 2 5 10 20 50 100
            var capacityParameter = new ProvisioningParameter("capacity", typeof(int))
            {
                Value = new BicepValue<int>(1)
            };
            construct.Add(capacityParameter);

            var service = new WebPubSubService(construct.Resource.GetBicepIdentifier())
            {
                Sku = new BillingInfoSku()
                {
                    Name = skuParameter,
                    Capacity = capacityParameter
                },
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(service);

            construct.Add(new ProvisioningOutput("endpoint", typeof(string)) { Value = BicepFunction.Interpolate($"https://{service.HostName}") });

            construct.Add(service.CreateRoleAssignment(WebPubSubBuiltInRole.WebPubSubServiceOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

            var resource = (AzureWebPubSubResource)construct.Resource;
            foreach (var setting in resource.Hubs)
            {
                var hubName = setting.Key;
                
                var hubBuilder = setting.Value;
                var hubResource = hubBuilder;
                var hub = new WebPubSubHub(Infrastructure.NormalizeIdentifierName(hubResource.Name))
                {
                    Name = setting.Key,
                    Parent = service
                };

                // create the hub settings with default values
                if (hub.Properties.Value == null)
                {
                    hub.Properties = new WebPubSubHubProperties();
                }

                // add to construct
                construct.Add(hub);
                // invoke the configure from AddEventHandler
                foreach (var eventHandlerConfigure in hubResource.EventHandlers)
                {
                    eventHandlerConfigure.Invoke(construct, hub);
                }
            }
        };

        var resource = new AzureWebPubSubResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Add hub settings
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="hubName">The hub name. Hub name is case-insensitive.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureWebPubSubHubResource> AddHub(this IResourceBuilder<AzureWebPubSubResource> builder, [ResourceName] string hubName)
    {
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
    public static IResourceBuilder<AzureWebPubSubHubResource> AddEventHandler(this IResourceBuilder<AzureWebPubSubHubResource> builder,
        ReferenceExpression.ExpressionInterpolatedStringHandler urlTemplateExpression, string userEventPattern = "*", string[]? systemEvents = null, UpstreamAuthSettings? authSettings = null)
    {
        var urlExpression = ReferenceExpression.Create(urlTemplateExpression);

        builder.Resource.EventHandlers.Add((construct, hub) =>
        {
            var hubName = hub.Name.Value;

            var hubProperties = hub.Properties.Value!;
            if (urlExpression.ManifestExpressions.Count == 0)
            {
                var eventHandler = GetWebPubSubEventHandler(urlExpression.Format, userEventPattern, systemEvents, authSettings);

                // when urlExpression is literal string, simply add
                hubProperties.EventHandlers.Add(eventHandler);
            }
            else
            {
                var count = hubProperties.EventHandlers.Count;
                var urlParameter = new ProvisioningParameter($"{hubName}_url_{count}", typeof(string));
                var eventHandler = GetWebPubSubEventHandler(urlParameter, userEventPattern, systemEvents, authSettings);
                hubProperties.EventHandlers.Add(eventHandler);
                construct.Add(urlParameter);

                builder.Resource.Parent.Parameters[urlParameter.IdentifierName] = urlExpression;
            }
        });
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
            handler.SystemEvents = [..systemEvents];
        }

        if (authSettings != null)
        {
            handler.Auth = new BicepValue<UpstreamAuthSettings>(authSettings);
        }
        return handler;
    }
}
