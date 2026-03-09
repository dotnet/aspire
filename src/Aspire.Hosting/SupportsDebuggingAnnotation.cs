// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Options passed to the launch configuration producer when creating debug launch configurations.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class LaunchConfigurationProducerOptions
{
    /// <summary>
    /// Gets the debug session run mode.
    /// </summary>
    public required string Mode { get; init; }
}

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, RequiredExtensionId = {LaunchConfigurationType,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class SupportsDebuggingAnnotation : IResourceAnnotation
{
    private SupportsDebuggingAnnotation(string launchConfigurationType, Action<Executable, LaunchConfigurationProducerOptions> launchConfigurationAnnotator)
    {
        LaunchConfigurationType = launchConfigurationType;
        LaunchConfigurationAnnotator = launchConfigurationAnnotator;
    }

    public string LaunchConfigurationType { get; }
    public Action<Executable, LaunchConfigurationProducerOptions> LaunchConfigurationAnnotator { get; }

    internal static SupportsDebuggingAnnotation Create<T>(string launchConfigurationType, Func<LaunchConfigurationProducerOptions, T> launchProfileProducer)
    {
        return new SupportsDebuggingAnnotation(launchConfigurationType, (exe, options) =>
        {
            exe.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, launchProfileProducer(options));
        });
    }
}