// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Associates a resource with a named launch group for local application runs.
/// </summary>
/// <param name="launchGroupName">The launch group name. Names are compared case-insensitively.</param>
/// <remarks>
/// A resource can have multiple launch group annotations. Use <c>WithLaunchGroup</c> or
/// <c>WithLaunchGroups</c> to configure membership.
/// </remarks>
public sealed class LaunchGroupAnnotation(string launchGroupName) : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the launch group.
    /// </summary>
    public string LaunchGroupName { get; } = ValidateName(launchGroupName);

    internal static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name != name.Trim() || name.Contains(',') || name.Contains(';'))
        {
            throw new ArgumentException("A launch group name cannot have surrounding whitespace or contain ',' or ';'.", nameof(name));
        }

        return name;
    }
}
