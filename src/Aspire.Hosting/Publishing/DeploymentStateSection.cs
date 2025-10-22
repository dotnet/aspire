// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a section of deployment state with version tracking for concurrency control.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeploymentStateSection"/> class.
/// </remarks>
/// <param name="sectionName">The name of the section.</param>
/// <param name="data">The JSON data for this section.</param>
/// <param name="version">The current version of this section.</param>
public sealed class DeploymentStateSection(string sectionName, JsonObject? data, long version)
{
    /// <summary>
    /// Gets the name of the state section.
    /// </summary>
    public string SectionName { get; } = sectionName;

    /// <summary>
    /// Gets the data stored in this section.
    /// </summary>
    /// <remarks>
    /// The <see cref="JsonObject"/> returned by this property is NOT thread-safe.
    /// Users should implement their own synchronization if concurrent access is required.
    /// </remarks>
    public JsonObject Data { get; } = data ?? [];

    /// <summary>
    /// Gets or sets the current version of this section.
    /// </summary>
    /// <remarks>
    /// This version is automatically incremented by <see cref="IDeploymentStateManager.SaveSectionAsync"/> 
    /// after a successful save, allowing multiple saves of the same section instance.
    /// </remarks>
    public long Version { get; set; } = version;
}
