// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure;

internal static class JsonExtensions
{
    public static JsonNode Prop(this JsonNode obj, string key)
    {
        var jsonObj = obj.AsObject();

        // Try to get the existing node
        var node = jsonObj[key];
        if (node is not null)
        {
            return node;
        }

        // Create a new node and add it
        // Note: This method should only be called from within a synchronized context
        // (e.g., within ProvisioningContext.WithDeploymentState) when multiple threads
        // may be accessing the same JsonObject concurrently
        node = new JsonObject();
        jsonObj.Add(key, node);

        return node;
    }
}
