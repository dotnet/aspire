// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
///
/// <param name="requiredExtensionId">The ID of the required extension that provides the debug adapter.</param>
/// <param name="launchConfigurationProducer">The launch configuration for the executable.</param>
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, RequiredExtensionId = {RequiredExtensionId,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class SupportsDebuggingAnnotation(
    string? requiredExtensionId,
    Func<ExecutableLaunchConfiguration> launchConfigurationProducer) : IResourceAnnotation
{
    /// <summary>
    /// Gets the required extension ID.
    /// </summary>
    public string? RequiredExtensionId { get; } = requiredExtensionId;

    /// <summary>
    /// Gets the base launch configuration for the executable.
    /// </summary>
    public Func<ExecutableLaunchConfiguration> LaunchConfigurationProducer { get; } = launchConfigurationProducer;
}
