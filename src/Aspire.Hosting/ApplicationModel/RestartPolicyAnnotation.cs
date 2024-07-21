// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the restart policy for a resource.
/// </summary>
public enum RestartPolicy : ushort
{
    /// <summary>
    /// Always restart the resource.
    /// </summary>
    Always,

    /// <summary>
    /// never restart the resource.
    /// </summary>
    Never,

    /// <summary>
    /// Restart only on failure.
    /// </summary>
    OnFailure,
}

/// <summary>
/// Represents an annotation indicating the restart policy for a resource.
/// </summary>
public class RestartPolicyAnnotation(RestartPolicy restartPolicy) : IResourceAnnotation
{
    /// <summary>
    /// Represents the restart policy for a resource.
    /// </summary>
    public RestartPolicy RestartPolicy { get; set; } = restartPolicy;
}
