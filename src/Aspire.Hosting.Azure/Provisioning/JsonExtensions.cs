// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure;

internal static class JsonExtensions
{
    public static JsonNode Prop(this JsonNode obj, string key)
    {
        var jsonObj = obj.AsObject();

        // Lock on the JsonObject to ensure thread-safe access when multiple
        // bicep resources are being provisioned in parallel
        lock (jsonObj)
        {
            // Try to get the existing node
            var node = jsonObj[key];
            if (node is not null)
            {
                return node;
            }

            // Create a new node and add it
            node = new JsonObject();
            jsonObj.Add(key, node);

            return node;
        }
    }
}
