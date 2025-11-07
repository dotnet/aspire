// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// A resource that represents a browser debugger configuration.
/// </summary>
public class BrowserDebuggerResource : ExecutableResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserDebuggerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="browser">The type of browser to debug (e.g., "msedge").</param>
    /// <param name="webRoot">The web root directory for the application.</param>
    /// <param name="workingDirectory">The working directory for the debugger.</param>
    /// <param name="url">The URL to launch in the browser.</param>
    /// <param name="configure">An action to configure additional debugger properties.</param>
    public BrowserDebuggerResource(string name, string browser, string webRoot, string workingDirectory, string url, Action<JavaScriptDebuggerProperties>? configure) : base(name, browser, workingDirectory)
    {
        DebuggerProperties = new JavaScriptDebuggerProperties
        {
            Type = browser,
            Name = $"{name} Debugger",
            WebRoot = webRoot,
            Url = url,
            WorkingDirectory = workingDirectory
        };

        configure?.Invoke(DebuggerProperties);
    }

    /// <summary>
    /// Gets the debugger properties for the browser.
    /// </summary>
    public JavaScriptDebuggerProperties DebuggerProperties { get; init; }
}
