// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Exposes the global contextual information for this invocation of the AppHost.
/// </summary>
public class DistributedApplicationExecutionContext
{
    internal DistributedApplicationExecutionContext(DistributedApplicationOperation operation)
    {
        Operation = operation;
    }

    /// <summary>
    /// The operation currently being performed by the AppHost.
    /// </summary>
    public DistributedApplicationOperation Operation { get; } 
}
