// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A class that can serialize Aspire service discovery information and save it in a JSON file.
/// </summary>
internal sealed class JsonServiceDiscoveryInfoSerializer(IJsonFileAccessor fileAccessor) : IServiceDiscoveryInfoSerializer
{
    private readonly IJsonFileAccessor _fileAccessor = fileAccessor ?? throw new ArgumentNullException(nameof(fileAccessor));

    /// <summary>
    /// Serializes the service discovery information for the <paramref name="resource"/> to JSON and saves it to the file accessed by the <see cref="IJsonFileAccessor"/> supplied in the constructor.
    /// </summary>
    /// <param name="resource">The resource for which to add service discovery information.</param>
    public void SerializeServiceDiscoveryInfo(IResourceWithEndpoints resource)
    {
        var json = _fileAccessor.ReadFileAsJson();

        var resourceName = resource.Name;
        var servicesJsonObject = EnsureServicesNode(json);
        var resourceJsonObject = RefreshResourceNodeOnServicesNode(servicesJsonObject, resourceName);

        if (resource.TryGetEndpoints(out var endpoints))
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.AllocatedEndpoint is null)
                {
                    throw new InvalidOperationException($"The distributed application's endpoints have not been allocated yet. The {nameof(SerializeServiceDiscoveryInfo)} method should be called after {nameof(AfterEndpointsAllocatedEvent)} has fired.");
                }

                var endpointName = endpoint.Name;
                var endpointUriStr = endpoint.AllocatedEndpoint!.UriString;

                var uriJsonArray = EnsureEndpointOnResourceNode(resourceJsonObject, endpointName);

                EnsureUriInArray(uriJsonArray, endpointUriStr);
            }
        }

        _fileAccessor.SaveJson(json);
    }

    private static JsonObject EnsureServicesNode(JsonObject appSettingsJsonObject)
    {
        return EnsureNode(appSettingsJsonObject, "Services", new JsonObject());
    }

    private static JsonObject RefreshResourceNodeOnServicesNode(JsonObject servicesJsonObject, string resourceName)
    {
        EnsureNode(servicesJsonObject, resourceName, new JsonObject());
        servicesJsonObject[resourceName]!.ReplaceWith(new JsonObject());

        return servicesJsonObject[resourceName]!.AsObject();
    }

    private static JsonArray EnsureEndpointOnResourceNode(JsonObject endpointJsonObject, string endpointName)
    {
        return EnsureNode(endpointJsonObject, endpointName, new JsonArray());
    }

    private static void EnsureUriInArray(JsonArray uriJsonArray, string endpointUriStr)
    {
        if (!uriJsonArray.Contains(endpointUriStr))
        {
            uriJsonArray.Add(endpointUriStr);
        }
    }

    private static T EnsureNode<T>(JsonObject parent, string nodeName, T valueIfNull)
        where T : JsonNode
    {
        if (parent[nodeName] is null)
        {
            parent.Add(nodeName, valueIfNull);
        }

        return (T)parent[nodeName]!;
    }
}
