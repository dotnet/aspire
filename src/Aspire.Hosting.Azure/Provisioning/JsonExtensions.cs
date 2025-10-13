// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure;

internal static class JsonExtensions
{
    // Use ConditionalWeakTable to associate a lock object with each JsonObject instance.
    // This avoids locking on external objects while ensuring thread-safe access when multiple
    // bicep resources are being provisioned in parallel.
    private static readonly ConditionalWeakTable<JsonObject, object> s_locks = new();

    public static JsonNode Prop(this JsonNode obj, string key)
    {
        var jsonObj = obj.AsObject();
        var lockObj = s_locks.GetOrCreateValue(jsonObj);

        lock (lockObj)
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
