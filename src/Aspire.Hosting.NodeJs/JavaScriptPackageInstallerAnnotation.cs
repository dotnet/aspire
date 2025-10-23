// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents an annotation for a JavaScript installer resource.
/// </summary>
public sealed class JavaScriptPackageInstallerAnnotation(ExecutableResource installerResource) : IResourceAnnotation
{
    /// <summary>
    /// The instance of the Installer resource used.
    /// </summary>
    public ExecutableResource Resource { get; } = installerResource;
}