// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta.Models;

internal class ResourceModel
{
    /// <summary>
    /// The resource type.
    /// </summary>
    /// <example>RedisResource</example>
    public required RoType ResourceType { get; init; }

    /// <summary>
    /// Extension methods for IResourceBuilder{T} of the current resource.
    /// </summary>
    /// <example>
    /// <code>
    /// IResourceBuilder&lt;RedisResource&gt; WithRedisCommander(this IResourceBuilder&lt;RedisResource&gt; builder, Action&lt;IResourceBuilder&lt;RedisCommanderResource&gt;&gt;? configureContainer = null, string? containerName = null)
    /// </code>
    /// </example>
    public List<RoMethod> IResourceTypeBuilderExtensionsMethods { get; } = [];

    public HashSet<RoType> ModelTypes { get; } = [];
}
