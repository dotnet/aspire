// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, RequiredExtensionId = {LaunchConfigurationType,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class SupportsDebuggingAnnotation : IResourceAnnotation
{
    private SupportsDebuggingAnnotation(string launchConfigurationType, Action<Executable, string> launchConfigurationAnnotator)
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
    internal Action<Executable, string> LaunchConfigurationAnnotator { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="SupportsDebuggingAnnotation"/> class.
    /// </summary>
    /// <typeparam name="T">The type of the launch configuration.</typeparam>
    /// <param name="launchConfigurationType">The type of launch configuration supported by the resource.</param>
    /// <param name="launchProfileProducer">The function that produces the launch configuration for the resource.</param>
    public static SupportsDebuggingAnnotation Create<T>(string launchConfigurationType, Func<string, T> launchProfileProducer)
    {
        return new SupportsDebuggingAnnotation(launchConfigurationType, (exe, mode) =>
        {
            exe.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, launchProfileProducer(mode));
        });
    }
}
