// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.EventHubs;

/// <summary>
/// Represents an annotation for updating the JSON content of a mounted document.
/// </summary>
internal sealed class ConfigJsonAnnotation : IResourceAnnotation
{
    public ConfigJsonAnnotation(Action<JsonNode> configure)
    {
        Configure = configure;
    }

    public Action<JsonNode> Configure { get; }
}
