// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These types are source shared between the CLI and the Aspire.Hosting projects.
// The CLI sets the types in its own namespace.
#if CLI
namespace Aspire.Cli.Backchannel;
#else
namespace Aspire.Hosting.ApplicationModel;
#endif

/// <summary>
/// Exception thrown when a resource enters a terminal failed state during a wait operation.
/// </summary>
public class ResourceFailedException : Exception
{
    /// <summary>
    /// Gets the name of the resource that failed.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the state that the resource entered when it failed.
    /// </summary>
    public string FailedState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFailedException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource that failed.</param>
    /// <param name="failedState">The state that the resource entered when it failed.</param>
    public ResourceFailedException(string resourceName, string failedState)
        : base($"Resource '{resourceName}' entered the '{failedState}' state and cannot become healthy.")
    {
        ResourceName = resourceName;
        FailedState = failedState;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFailedException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource that failed.</param>
    /// <param name="failedState">The state that the resource entered when it failed.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ResourceFailedException(string resourceName, string failedState, string message)
        : base(message)
    {
        ResourceName = resourceName;
        FailedState = failedState;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFailedException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource that failed.</param>
    /// <param name="failedState">The state that the resource entered when it failed.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceFailedException(string resourceName, string failedState, string message, Exception innerException)
        : base(message, innerException)
    {
        ResourceName = resourceName;
        FailedState = failedState;
    }
}