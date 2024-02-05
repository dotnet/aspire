// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an OpenAI resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class OpenAIResource(string name) : Resource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the connection string for the OpenAI resource.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the connection string for the OpenAI service.
    /// </summary>
    /// <returns>The connection string for the OpenAI service.</returns>
    string? IResourceWithConnectionString.GetConnectionString() => ConnectionString;
}
