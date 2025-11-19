// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Marks a resource as containing debugger properties.
/// </summary>
/// <remarks>
/// <para>
/// This annotation indicates that the resource contains a custom debugger configuration.
/// When this annotation is present, the resource will be configured with appropriate debug launch configurations.
/// </para>
/// </remarks>
public sealed class ExecutableDebuggerPropertiesAnnotation<T>(Action<T> configureDebugProperties) : IResourceAnnotation
    where T : VSCodeDebuggerProperties
{
    /// <summary>
    /// Gets the action to configure the debugger properties.
    /// </summary>
    public Action<T> ConfigureDebuggerProperties { get; } = configureDebugProperties;
}
