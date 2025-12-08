// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation to register a callback to be invoked during resource finalization. Callbacks are executed in reverse order
/// of their registration immediately after the BeforeStartEvent is complete.
/// </summary>
/// <remarks>
/// This annotation is used to register a callback that will be invoked immediately after the BeforeStartEvent is complete.
/// Callbacks are executed in reverse order of their registration and it is safe to modify resource annotation state during
/// this callback, but any additional <see cref="FinalizeResourceConfigurationCallbackAnnotation"/> annotations added in the
/// callback will be ignored.
/// </remarks>
[Experimental("ASPIRELIFECYCLE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class FinalizeResourceConfigurationCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The callback to be invoked during resource finalization.
    /// </summary>
    public required Func<FinalizeResourceConfigurationCallbackAnnotationContext, Task> Callback { get; init; }
}

/// <summary>
/// Context for a finalize resource configuration callback annotation.
/// </summary>
/// <remarks>
/// This context provides access to the resource and its execution context, as well as a cancellation token.
/// </remarks>
[Experimental("ASPIRELIFECYCLE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class FinalizeResourceConfigurationCallbackAnnotationContext
{
    /// <summary>
    /// The resource associated with the callback.
    /// </summary>
    public required IResource Resource { get; init; }

    /// <summary>
    /// The execution context for the callback.
    /// </summary>
    public required DistributedApplicationExecutionContext ExecutionContext { get; init; }

    /// <summary>
    /// The cancellation token for the callback.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}