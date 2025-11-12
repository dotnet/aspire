// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource supports debugging by the Aspire extension.
/// This annotation is used to configure launch configurations for debugging resources in development environments.
/// </summary>
/// <remarks>
/// This annotation is experimental and subject to change in future releases.
/// Use <see cref="Create{T}"/> to create an instance of this annotation with a specific launch configuration type
/// and profile producer function.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, LaunchConfigurationType = {LaunchConfigurationType,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class SupportsDebuggingAnnotation : IResourceAnnotation
{
    private SupportsDebuggingAnnotation(string launchConfigurationType, Action<Executable, string> launchConfigurationAnnotator)
    {
        LaunchConfigurationType = launchConfigurationType;
        LaunchConfigurationAnnotator = launchConfigurationAnnotator;
    }

    /// <summary>
    /// Gets the type of launch configuration required for debugging this resource.
    /// This corresponds to the extension ID that provides the debugging support.
    /// </summary>
    public string LaunchConfigurationType { get; }

    /// <summary>
    /// Gets the action that annotates the executable with launch configuration details.
    /// </summary>
    internal Action<Executable, string> LaunchConfigurationAnnotator { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SupportsDebuggingAnnotation"/> with the specified launch configuration type
    /// and launch profile producer function.
    /// </summary>
    /// <typeparam name="T">The type of launch profile object produced by the <paramref name="launchProfileProducer"/>.</typeparam>
    /// <param name="launchConfigurationType">The type of launch configuration (typically an extension ID) required for debugging.</param>
    /// <param name="launchProfileProducer">A function that produces a launch profile object for the specified debug mode.</param>
    /// <returns>A new <see cref="SupportsDebuggingAnnotation"/> instance.</returns>
    public static SupportsDebuggingAnnotation Create<T>(string launchConfigurationType, Func<string, T> launchProfileProducer)
    {
        return new SupportsDebuggingAnnotation(launchConfigurationType, (exe, mode) =>
        {
            exe.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, launchProfileProducer(mode));
        });
    }
}
