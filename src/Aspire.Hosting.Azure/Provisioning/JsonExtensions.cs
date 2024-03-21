// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure;

internal static class JsonExtensions
{
    public static JsonNode Prop(this JsonNode obj, string key)
    {
        var node = obj[key];
        if (node is not null)
        {
            return node;
        }

        node = new JsonObject();
        obj.AsObject().Add(key, node);
        return node;
    }
}
