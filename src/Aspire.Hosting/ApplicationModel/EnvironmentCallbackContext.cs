// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback context for environment variables associated with a publisher.
/// </summary>
/// <param name="publisherName">The name of the publisher.</param>
/// <param name="environmentVariables">The environment variables associated with the publisher.</param>
public class EnvironmentCallbackContext(string publisherName, Dictionary<string, string>? environmentVariables = null)
{
    /// <summary>
    /// Gets the environment variables associated with the callback context.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = environmentVariables ?? new();

    /// <summary>
    /// Gets the name of the publisher associated with the callback context.
    /// </summary>
    public string PublisherName { get; } = publisherName;
}
