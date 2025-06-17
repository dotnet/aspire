// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Describes the context in which the AppHost is being executed.
/// </summary>
public enum DistributedApplicationOperation
{
    /// <summary>
    /// AppHost is being run for the purpose of debugging locally.
    /// </summary>
    Run,

    /// <summary>
    /// AppHost is being run for the purpose of publishing a manifest for deployment.
    /// </summary>
    Publish,

    /// <summary>
    /// AppHost is being run for the purpose of executing a tool.
    /// </summary>
    Tool
}
