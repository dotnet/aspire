// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Represents environment variables to be set for Poetry operations.
/// </summary>
/// <param name="environmentVariables">The environment variables to set for Poetry.</param>
internal sealed class PoetryEnvironmentAnnotation((string key, string value)[] environmentVariables) : IResourceAnnotation
{
    /// <summary>
    /// Gets the environment variables to be set for Poetry operations.
    /// </summary>
    public (string key, string value)[] EnvironmentVariables { get; } = environmentVariables;
}
