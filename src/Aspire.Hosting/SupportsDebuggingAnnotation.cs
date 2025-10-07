// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, RequiredExtensionId = {RequiredExtensionId,nq}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class SupportsDebuggingAnnotation : IResourceAnnotation
{
    private SupportsDebuggingAnnotation(string? requiredExtensionId,
        Action<Executable, string> launchConfigurationAnnotator)
    {
        RequiredExtensionId = requiredExtensionId;
        LaunchConfigurationAnnotator = launchConfigurationAnnotator;
    }

    public string? RequiredExtensionId { get; }
    public Action<Executable, string> LaunchConfigurationAnnotator { get; }

    internal static SupportsDebuggingAnnotation Create<T>(string? requiredExtensionId, Func<string, T> launchProfileProducer)
    {
        return new SupportsDebuggingAnnotation(requiredExtensionId, (exe, mode) =>
        {
            exe.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, launchProfileProducer(mode));
        });
    }
}
