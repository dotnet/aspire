// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a name/value pair, used to satisfy the <c>docker builder --build-arg &lt;NAME&gt;[=&lt;VALUE&gt;]</c> command switch.
/// For more information, see <a href="https://docs.docker.com/reference/cli/docker/image/build/#build-arg"></a>.
/// </summary>
/// <param name="name">The required name of the arg.</param>
/// <param name="value">The optional value of the arg, when omitted the value is populated from the corresponding environment variable.</param>
[Obsolete("Use DockerfileBuildAnnotation to define docker build arguments.")]
public sealed class DockerBuildArg(string name, object? value = null)
{
    /// <summary>
    /// Gets or initializes the name part of the <c>docker builder --build-arg &lt;NAME&gt;[=&lt;VALUE&gt;]</c>.
    /// </summary>
    public string Name { get; init; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets or initializes the value part of the <c>docker builder --build-arg &lt;NAME&gt;[=&lt;VALUE&gt;]</c>.
    /// </summary>
    public object? Value { get; init; } = value;
}
