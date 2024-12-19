// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using YamlDotNet.Serialization;

namespace Aspire.Hosting.Dapr.Models.ComponentSpec;

/// <summary>
/// A key value pair for metadata
/// </summary>
[Serializable]
public abstract class MetadataValue
{
    /// <summary>
    /// The name / key of the metadata
    /// </summary>
    [YamlMember(Order = -1)]
    public required string Name { get; init; }
}
