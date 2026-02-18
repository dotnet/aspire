// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Non-generic interface for debugger properties annotations, enabling IDE-agnostic annotation lookup.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IDebuggerPropertiesAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the IDE type this annotation is for (e.g., "vscode"). If <see langword="null"/>, applies to all IDEs.
    /// </summary>
    string? IdeType { get; }

    /// <summary>
    /// Configures the debugger properties if the runtime type matches the expected type.
    /// </summary>
    /// <param name="debuggerProperties">The debugger properties to configure.</param>
    void ConfigureDebuggerProperties(DebugAdapterProperties debuggerProperties);
}

/// <summary>
/// Marks a resource as containing debugger properties.
/// </summary>
/// <remarks>
/// <para>
/// This annotation indicates that the resource contains a custom debugger configuration.
/// When this annotation is present, the resource will be configured with appropriate debug launch configurations.
/// </para>
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ExecutableDebuggerPropertiesAnnotation<T>(Action<T> configureDebugProperties, string? ideType = null) : IDebuggerPropertiesAnnotation
    where T : DebugAdapterProperties
{
    /// <inheritdoc />
    public string? IdeType { get; } = ideType;

    /// <summary>
    /// Gets the action to configure the debugger properties.
    /// </summary>
    public Action<T> ConfigureDebuggerPropertiesTyped { get; } = configureDebugProperties;

    /// <inheritdoc />
    public void ConfigureDebuggerProperties(DebugAdapterProperties debuggerProperties)
    {
        if (debuggerProperties is T typed)
        {
            ConfigureDebuggerPropertiesTyped(typed);
        }
    }
}
