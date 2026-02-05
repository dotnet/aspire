// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Provides JSON serializer options configured for the Debug Adapter Protocol.
/// </summary>
public static class DebugAdapterJsonOptions
{
    /// <summary>
    /// Gets JSON serializer options using the default Debug Adapter JSON context.
    /// </summary>
    /// <remarks>
    /// This property returns a new <see cref="JsonSerializerOptions"/> instance each time.
    /// Consider caching the result if you call this frequently.
    /// </remarks>
    public static JsonSerializerOptions Default => Create(DefaultDebugAdapterJsonContext.Default);

    /// <summary>
    /// Creates JSON serializer options configured for Debug Adapter messages.
    /// </summary>
    /// <param name="typeInfoResolver">
    /// Type info resolver for JSON serialization. Use <see cref="DefaultDebugAdapterJsonContext.Default"/>
    /// for standard Debug Adapter types, or provide your own context if you've extended the types.
    /// </param>
    /// <returns>Configured JsonSerializerOptions instance.</returns>
    public static JsonSerializerOptions Create(IJsonTypeInfoResolver typeInfoResolver)
    {
        ArgumentNullException.ThrowIfNull(typeInfoResolver);

        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowOutOfOrderMetadataProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = typeInfoResolver
        };
    }
}
