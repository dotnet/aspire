// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation to register a callback to be invoked during resource finalization. Callbacks are executed in reverse order
/// of their registration immediately after the BeforeStartEvent is complete.
/// </summary>
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