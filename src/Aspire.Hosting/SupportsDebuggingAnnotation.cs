// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, RequiredExtensionId = {LaunchConfigurationType,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class SupportsDebuggingAnnotation : IResourceAnnotation
{
    private SupportsDebuggingAnnotation(string launchConfigurationType, Action<Executable, LaunchConfigurationProducerOptions> launchConfigurationAnnotator)
    {
        LaunchConfigurationType = launchConfigurationType;
        LaunchConfigurationAnnotator = launchConfigurationAnnotator;
    }

    /// <summary>
    /// Gets the type of launch configuration supported by the resource.
    /// </summary>
    public string LaunchConfigurationType { get; }

    /// <summary>
    /// Gets the action that annotates the launch configuration for the resource.
    /// </summary>
    /// <returns>Whether the annotation was applied successfully. If this throws, the resource will not be launched in IDE</returns>
    internal Action<Executable, LaunchConfigurationProducerOptions> LaunchConfigurationAnnotator { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="SupportsDebuggingAnnotation"/> class.
    /// </summary>
    /// <typeparam name="T">The type of the launch configuration.</typeparam>
    /// <param name="launchConfigurationType">The type of launch configuration supported by the resource.</param>
    /// <param name="launchProfileProducer">The function that produces the launch configuration for the resource.</param>
    public static SupportsDebuggingAnnotation Create<T>(string launchConfigurationType, Func<LaunchConfigurationProducerOptions, T> launchProfileProducer)
        where T : ExecutableLaunchConfiguration
    {
        return new SupportsDebuggingAnnotation(launchConfigurationType, (exe, options) =>
        {
            exe.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, launchProfileProducer(options));
        });
    }
}

/// <summary>
/// Provides options for producing launch configurations for debugging resources.
/// </summary>
public sealed class LaunchConfigurationProducerOptions
{
    /// <summary>
    /// The mode for the launch configuration. Possible values include Debug or NoDebug.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// The logger used for debug console output.
    /// </summary>
    public required ILogger DebugConsoleLogger { get; init; }

    /// <summary>
    /// Internal hook to allow further configuration of the launch configuration after creation, only for project resources.
    /// </summary>
    internal Action<ExecutableLaunchConfiguration>? AdditionalConfiguration { get; set; }
}
