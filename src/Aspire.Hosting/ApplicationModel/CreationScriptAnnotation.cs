// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for defining a script to create a resource.
/// </summary>
public sealed class CreationScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreationScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">The script used to create the resource.</param>
    public CreationScriptAnnotation(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        Script = script;
    }

    /// <summary>
    /// Gets the script used to create the resource.
    /// </summary>
    public string Script { get; }
}
